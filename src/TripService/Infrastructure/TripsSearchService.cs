using Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public class TripsSearchService : ITripsSearchService
{
    private readonly string _connectionString;
    private readonly IRoutingEngine _routing;

    public TripsSearchService(IConfiguration config, IRoutingEngine routing)
    {
        _connectionString = config.GetConnectionString("TripConnection")
            ?? throw new InvalidOperationException("TripConnection is not configured.");
        _routing = routing;
    }

    public async Task<SyncSearchResultDTO> SearchAsync(SearchTripsQueryDTO q, CancellationToken ct = default)
    {
        if (!DateOnly.TryParse(q.DateFrom, out var dateFrom))
            throw new ValidationException("dateFrom must be in YYYY-MM-DD format.");
        if (!DateOnly.TryParse(q.DateTo, out var dateTo))
            throw new ValidationException("dateTo must be in YYYY-MM-DD format.");
        if (dateTo < dateFrom)
            throw new ValidationException("dateTo must be >= dateFrom.");

        var page     = Math.Max(q.Page, 1);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var minSeats = Math.Max(q.MinSeats, 1);

        var passengerSrc = new LatLngDTO { Lat = q.SourceLat, Lng = q.SourceLng };
        var passengerTgt = new LatLngDTO { Lat = q.TargetLat, Lng = q.TargetLng };

        // ── Phase 1: PostGIS pre-filter ────────────────────────────────────────
        var candidates = await Phase1Async(
            passengerSrc, passengerTgt,
            dateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            dateTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
            q.MaxPrice, minSeats, ct);

        if (candidates.Count == 0)
            return new SyncSearchResultDTO { Page = page, PageSize = pageSize };

        // ── Phase 2: Valhalla matrix validation ───────────────────────────────
        var validated = await Phase2Async(candidates, passengerSrc, passengerTgt, ct);

        // ── Phase 3: sort, count, paginate ────────────────────────────────────
        var sortByPrice = q.SortBy.Equals("price", StringComparison.OrdinalIgnoreCase);
        var sorted = sortByPrice
            ? validated.OrderBy(r => r.Summary.PricePerSeat).ThenBy(r => r.Summary.DepartureTime)
            : validated.OrderBy(r => r.Summary.DepartureTime);

        var all   = sorted.ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(r => r.Summary).ToList();

        return new SyncSearchResultDTO
        {
            Items      = items,
            Page       = page,
            PageSize   = pageSize,
            TotalCount = all.Count
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 1
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<List<Candidate>> Phase1Async(
        LatLngDTO src, LatLngDTO tgt,
        DateTime dateFrom, DateTime dateTo,
        decimal? maxPrice, int minSeats,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                t.id,
                t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m,
                t.max_detour_m,
                t.departure_time,
                t.price_per_seat,
                t.available_seats,
                (SELECT COUNT(*) FROM trip_passenger WHERE trip_id = t.id) AS passenger_count
            FROM trip t
            WHERE t.status = 'ACTIVE'
              AND t.departure_time >= @dateFrom
              AND t.departure_time <= @dateTo
              AND (@maxPrice IS NULL OR t.price_per_seat <= @maxPrice)
              AND (t.available_seats
                   - (SELECT COUNT(*) FROM trip_passenger WHERE trip_id = t.id)) >= @minSeats
              AND ST_DWithin(
                    t.route_polyline,
                    ST_SetSRID(ST_MakePoint(@srcLng, @srcLat), 4326)::geography,
                    t.max_detour_m)
              AND ST_DWithin(
                    t.route_polyline,
                    ST_SetSRID(ST_MakePoint(@tgtLng, @tgtLat), 4326)::geography,
                    t.max_detour_m)
            LIMIT 200
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("dateFrom", dateFrom);
        cmd.Parameters.AddWithValue("dateTo",   dateTo);
        cmd.Parameters.Add(new NpgsqlParameter("maxPrice", NpgsqlTypes.NpgsqlDbType.Numeric) { Value = (object?)maxPrice ?? DBNull.Value });
        cmd.Parameters.AddWithValue("minSeats", minSeats);
        cmd.Parameters.AddWithValue("srcLat", src.Lat);
        cmd.Parameters.AddWithValue("srcLng", src.Lng);
        cmd.Parameters.AddWithValue("tgtLat", tgt.Lat);
        cmd.Parameters.AddWithValue("tgtLng", tgt.Lng);

        var result = new List<Candidate>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new Candidate(
                Id             : reader.GetGuid(reader.GetOrdinal("id")).ToString(),
                DriverId       : reader.GetGuid(reader.GetOrdinal("driver_user_id")).ToString(),
                SourceLat      : reader.GetDouble(reader.GetOrdinal("source_lat")),
                SourceLng      : reader.GetDouble(reader.GetOrdinal("source_lng")),
                TargetLat      : reader.GetDouble(reader.GetOrdinal("target_lat")),
                TargetLng      : reader.GetDouble(reader.GetOrdinal("target_lng")),
                RouteDistanceM : reader.GetInt32(reader.GetOrdinal("route_distance_m")),
                MaxDetourM     : reader.GetInt32(reader.GetOrdinal("max_detour_m")),
                DepartureTime  : reader.GetDateTime(reader.GetOrdinal("departure_time")),
                PricePerSeat   : reader.GetDecimal(reader.GetOrdinal("price_per_seat")),
                AvailableSeats : reader.GetInt16(reader.GetOrdinal("available_seats")),
                PassengerCount : reader.GetInt64(reader.GetOrdinal("passenger_count"))));
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<List<ValidatedResult>> Phase2Async(
        List<Candidate> candidates,
        LatLngDTO passengerSrc,
        LatLngDTO passengerTgt,
        CancellationToken ct)
    {
        var tripSources = candidates
            .Select(c => new LatLngDTO { Lat = c.SourceLat, Lng = c.SourceLng })
            .ToArray();
        var tripTargets = candidates
            .Select(c => new LatLngDTO { Lat = c.TargetLat, Lng = c.TargetLng })
            .ToArray();

        // 3 matrix calls in parallel, regardless of candidate count
        var matrixTasks = await Task.WhenAll(
            _routing.GetMatrixAsync(tripSources,         new[] { passengerSrc }, ct),  // leg1[i][0]
            _routing.GetMatrixAsync(new[] { passengerTgt }, tripTargets,          ct),  // leg2[0][i]
            _routing.GetMatrixAsync(new[] { passengerSrc }, new[] { passengerTgt }, ct) // leg3[0][0]
        );

        var leg1Matrix = matrixTasks[0];
        var leg2Matrix = matrixTasks[1];
        var leg3 = leg2Matrix[0][0] != null ? matrixTasks[2][0][0] : null; // dist(passengerSrc → passengerTgt)

        var validated = new List<ValidatedResult>();

        for (int i = 0; i < candidates.Count; i++)
        {
            var c    = candidates[i];
            var leg1 = leg1Matrix[i][0];   // dist(trip.source → passenger.source)
            var leg2 = leg2Matrix[0][i];   // dist(passenger.target → trip.target)

            if (leg1 is null || leg2 is null || leg3 is null)
                continue; // unreachable

            var detour = leg1.Value + leg3.Value + leg2.Value - c.RouteDistanceM;

            if (detour < 0 || detour > c.MaxDetourM)
                continue;

            validated.Add(new ValidatedResult(
                Summary: new TripSummaryV1DTO
                {
                    Id                  = c.Id,
                    DriverId            = c.DriverId,
                    Source              = new LatLngDTO { Lat = c.SourceLat, Lng = c.SourceLng },
                    Target              = new LatLngDTO { Lat = c.TargetLat, Lng = c.TargetLng },
                    DepartureTime       = c.DepartureTime,
                    PricePerSeat        = c.PricePerSeat,
                    AvailableSeats      = (int)(c.AvailableSeats - c.PassengerCount),
                    MaxDetourMeters     = c.MaxDetourM,
                    ActualDetourMeters  = detour
                }
            ));
        }

        return validated;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private record Candidate(
        string Id, string DriverId,
        double SourceLat, double SourceLng,
        double TargetLat, double TargetLng,
        int RouteDistanceM, int MaxDetourM,
        DateTime DepartureTime,
        decimal PricePerSeat,
        short AvailableSeats,
        long PassengerCount);

    private record ValidatedResult(TripSummaryV1DTO Summary);
}
