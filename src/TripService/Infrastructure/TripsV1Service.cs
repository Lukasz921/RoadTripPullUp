using Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public class TripsV1Service : ITripsV1Service
{
    private readonly string _connectionString;
    private readonly IRoutingEngine _routing;
    private readonly IJobStore _jobStore;

    public TripsV1Service(IConfiguration config, IRoutingEngine routing, IJobStore jobStore)
    {
        _connectionString = config.GetConnectionString("TripConnection")
            ?? throw new InvalidOperationException("TripConnection is not configured.");
        _routing  = routing;
        _jobStore = jobStore;
    }

    public async Task<TripV1DTO> CreateTripAsync(CreateTripV1DTO dto, string driverId)
    {
        if (dto.DepartureTime.ToUniversalTime() <= DateTime.UtcNow)
            throw new ValidationException("departureTime must be in the future.");
        if (dto.MaxDetourMeters <= 0)
            throw new ValidationException("maxDetourMeters must be positive.");
        if (dto.PricePerSeat <= 0)
            throw new ValidationException("pricePerSeat must be positive.");
        if (dto.AvailableSeats <= 0)
            throw new ValidationException("availableSeats must be positive.");

        var route = await _routing.GetRouteAsync(dto.Source, dto.Target);

        const string sql = """
            INSERT INTO trip (
                driver_user_id,
                source_geog,
                target_geog,
                route_polyline,
                route_distance_m,
                route_duration_s,
                max_detour_m,
                departure_time,
                price_per_seat,
                available_seats
            )
            VALUES (
                @driverId,
                ST_SetSRID(ST_MakePoint(@srcLng, @srcLat), 4326)::geography,
                ST_SetSRID(ST_MakePoint(@tgtLng, @tgtLat), 4326)::geography,
                ST_GeomFromText(@polylineWkt, 4326)::geography,
                @distanceM,
                @durationS,
                @maxDetourM,
                @departureTime,
                @pricePerSeat,
                @availableSeats
            )
            RETURNING
                id,
                driver_user_id,
                ST_Y(source_geog::geometry) AS source_lat,
                ST_X(source_geog::geometry) AS source_lng,
                ST_Y(target_geog::geometry) AS target_lat,
                ST_X(target_geog::geometry) AS target_lng,
                route_distance_m,
                route_duration_s,
                max_detour_m,
                departure_time,
                price_per_seat,
                available_seats,
                status::text AS status,
                created_at
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("driverId", Guid.Parse(driverId));
        cmd.Parameters.AddWithValue("srcLat", dto.Source.Lat);
        cmd.Parameters.AddWithValue("srcLng", dto.Source.Lng);
        cmd.Parameters.AddWithValue("tgtLat", dto.Target.Lat);
        cmd.Parameters.AddWithValue("tgtLng", dto.Target.Lng);
        cmd.Parameters.AddWithValue("polylineWkt", route.PolylineWkt);
        cmd.Parameters.AddWithValue("distanceM", route.DistanceM);
        cmd.Parameters.AddWithValue("durationS", route.DurationS);
        cmd.Parameters.AddWithValue("maxDetourM", dto.MaxDetourMeters);
        cmd.Parameters.AddWithValue("departureTime", DateTime.SpecifyKind(dto.DepartureTime.ToUniversalTime(), DateTimeKind.Utc));
        cmd.Parameters.AddWithValue("pricePerSeat", dto.PricePerSeat);
        cmd.Parameters.AddWithValue("availableSeats", (short)dto.AvailableSeats);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return MapRow(reader, passengerIds: new());
    }

    public async Task<TripV1DTO> GetTripAsync(string tripId)
    {
        if (!Guid.TryParse(tripId, out var id))
            throw new NotFoundException($"Trip '{tripId}' not found.");

        const string sql = """
            SELECT
                t.id,
                t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m,
                t.route_duration_s,
                t.max_detour_m,
                t.departure_time,
                t.price_per_seat,
                t.available_seats,
                t.status::text AS status,
                t.created_at,
                COALESCE(
                    ARRAY_AGG(tp.passenger_user_id ORDER BY tp.joined_at) FILTER (WHERE tp.passenger_user_id IS NOT NULL),
                    '{}'::uuid[]
                ) AS passenger_ids
            FROM trip t
            LEFT JOIN trip_passenger tp ON tp.trip_id = t.id
            WHERE t.id = @id
            GROUP BY t.id, t.driver_user_id, t.source_geog, t.target_geog,
                     t.route_distance_m, t.route_duration_s, t.max_detour_m,
                     t.departure_time, t.price_per_seat, t.available_seats,
                     t.status, t.created_at
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new NotFoundException($"Trip '{tripId}' not found.");

        return MapRowWithPassengers(reader);
    }

    public async Task<PagedTripsDTO> GetMyTripsAsync(string driverId, int page, int pageSize)
    {
        if (!Guid.TryParse(driverId, out var driverGuid))
            return new PagedTripsDTO { Page = page, PageSize = pageSize };

        const string sql = """
            SELECT
                t.id,
                t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m,
                t.route_duration_s,
                t.max_detour_m,
                t.departure_time,
                t.price_per_seat,
                t.available_seats,
                t.status::text AS status,
                t.created_at,
                COALESCE(
                    ARRAY_AGG(tp.passenger_user_id ORDER BY tp.joined_at) FILTER (WHERE tp.passenger_user_id IS NOT NULL),
                    '{}'::uuid[]
                ) AS passenger_ids,
                COUNT(*) OVER() AS total_count
            FROM trip t
            LEFT JOIN trip_passenger tp ON tp.trip_id = t.id
            WHERE t.driver_user_id = @driverId
              AND t.status = 'ACTIVE'
            GROUP BY t.id, t.driver_user_id, t.source_geog, t.target_geog,
                     t.route_distance_m, t.route_duration_s, t.max_detour_m,
                     t.departure_time, t.price_per_seat, t.available_seats,
                     t.status, t.created_at
            ORDER BY t.departure_time ASC
            LIMIT @pageSize OFFSET @offset
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("driverId", driverGuid);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        return await ReadPagedTrips(cmd, page, pageSize);
    }

    public async Task<PagedTripsDTO> GetMyPassengerTripsAsync(string userId, int page, int pageSize)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return new PagedTripsDTO { Page = page, PageSize = pageSize };

        const string sql = """
            SELECT
                t.id,
                t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m,
                t.route_duration_s,
                t.max_detour_m,
                t.departure_time,
                t.price_per_seat,
                t.available_seats,
                t.status::text AS status,
                t.created_at,
                COALESCE(
                    ARRAY_AGG(tp_all.passenger_user_id ORDER BY tp_all.joined_at) FILTER (WHERE tp_all.passenger_user_id IS NOT NULL),
                    '{}'::uuid[]
                ) AS passenger_ids,
                COUNT(*) OVER() AS total_count
            FROM trip t
            INNER JOIN trip_passenger tp_me ON tp_me.trip_id = t.id AND tp_me.passenger_user_id = @userId
            LEFT JOIN trip_passenger tp_all ON tp_all.trip_id = t.id
            WHERE t.status = 'ACTIVE'
            GROUP BY t.id, t.driver_user_id, t.source_geog, t.target_geog,
                     t.route_distance_m, t.route_duration_s, t.max_detour_m,
                     t.departure_time, t.price_per_seat, t.available_seats,
                     t.status, t.created_at
            ORDER BY t.departure_time ASC
            LIMIT @pageSize OFFSET @offset
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userGuid);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        return await ReadPagedTrips(cmd, page, pageSize);
    }

    public async Task AddPassengerAsync(string tripId, string driverId, string passengerId)
    {
        if (!Guid.TryParse(tripId, out var tripGuid))
            throw new NotFoundException($"Trip '{tripId}' not found.");
        if (!Guid.TryParse(driverId, out var driverGuid))
            throw new ForbiddenException("Invalid driver identity.");
        if (!Guid.TryParse(passengerId, out var passengerGuid))
            throw new ValidationException("passengerId is not a valid UUID.");
        if (driverGuid == passengerGuid)
            throw new ValidationException("Driver cannot be added as a passenger on their own trip.");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        const string checkSql = """
            SELECT
                t.driver_user_id,
                t.status::text,
                t.available_seats,
                (SELECT COUNT(*) FROM trip_passenger WHERE trip_id = t.id) AS passenger_count,
                EXISTS(SELECT 1 FROM trip_passenger WHERE trip_id = t.id AND passenger_user_id = @passengerId) AS already_joined
            FROM trip t
            WHERE t.id = @id
            FOR UPDATE
            """;

        await using var checkCmd = new NpgsqlCommand(checkSql, conn, tx);
        checkCmd.Parameters.AddWithValue("id", tripGuid);
        checkCmd.Parameters.AddWithValue("passengerId", passengerGuid);

        await using var r = await checkCmd.ExecuteReaderAsync();
        if (!await r.ReadAsync())
        {
            await tx.RollbackAsync();
            throw new NotFoundException($"Trip '{tripId}' not found.");
        }

        var tripDriverId   = r.GetGuid(r.GetOrdinal("driver_user_id"));
        var status         = r.GetString(r.GetOrdinal("status"));
        var availableSeats = r.GetInt16(r.GetOrdinal("available_seats"));
        var passengerCount = r.GetInt64(r.GetOrdinal("passenger_count"));
        var alreadyJoined  = r.GetBoolean(r.GetOrdinal("already_joined"));
        await r.CloseAsync();

        if (tripDriverId != driverGuid)
            throw new ForbiddenException("Only the trip driver can add passengers.");
        if (status != "ACTIVE")
            throw new ValidationException("Trip is not active.");
        if (alreadyJoined)
            throw new ValidationException("This user is already a passenger on this trip.");
        if (passengerCount >= availableSeats)
            throw new SeatUnavailableException("No seats available on this trip.");

        await using var insertCmd = new NpgsqlCommand(
            "INSERT INTO trip_passenger (trip_id, passenger_user_id) VALUES (@id, @passengerId)",
            conn, tx);
        insertCmd.Parameters.AddWithValue("id", tripGuid);
        insertCmd.Parameters.AddWithValue("passengerId", passengerGuid);
        await insertCmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }

    public async Task DeleteTripAsync(string tripId, string driverId)
    {
        if (!Guid.TryParse(tripId, out var id))
            throw new NotFoundException($"Trip '{tripId}' not found.");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var selectCmd = new NpgsqlCommand(
            "SELECT driver_user_id FROM trip WHERE id = @id", conn);
        selectCmd.Parameters.AddWithValue("id", id);
        var ownerObj = await selectCmd.ExecuteScalarAsync();

        if (ownerObj is null)
            throw new NotFoundException($"Trip '{tripId}' not found.");
        if (ownerObj.ToString() != driverId)
            throw new ForbiddenException("You are not the driver of this trip.");

        await using var deleteCmd = new NpgsqlCommand("DELETE FROM trip WHERE id = @id", conn);
        deleteCmd.Parameters.AddWithValue("id", id);
        await deleteCmd.ExecuteNonQueryAsync();
    }

    public async Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsV1RequestDTO dto, string userId)
    {
        if (!DateOnly.TryParse(dto.DateFrom, out _))
            throw new ValidationException("dateFrom must be in YYYY-MM-DD format.");
        if (!DateOnly.TryParse(dto.DateTo, out _))
            throw new ValidationException("dateTo must be in YYYY-MM-DD format.");

        var query = new SearchTripsQueryDTO
        {
            SourceLat = dto.Source.Lat,
            SourceLng = dto.Source.Lng,
            TargetLat = dto.Target.Lat,
            TargetLng = dto.Target.Lng,
            DateFrom  = dto.DateFrom,
            DateTo    = dto.DateTo,
            MaxPrice  = dto.MaxPrice,
            MinSeats  = dto.MinSeats,
            SortBy    = dto.SortBy,
            Page      = dto.Page,
            PageSize  = dto.PageSize
        };

        var jobId = await _jobStore.EnqueueAsync(userId, query);

        return new SearchJobCreatedDTO
        {
            JobId                = jobId,
            Status               = "pending",
            StatusUrl            = $"/api/v1/trips/search/{jobId}",
            EstimatedDurationMs  = 3_000
        };
    }

    public async Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId)
    {
        var job = await _jobStore.GetJobAsync(jobId);

        if (job == null || job.UserId != userId)
            throw new NotFoundException($"Search job {jobId} not found.");

        if (job.Status is "pending" or "processing")
            return new SearchJobPollResult
            {
                IsProcessing = true,
                Progress = new SearchJobProgressDTO { JobId = jobId, Status = job.Status }
            };

        if (job.Status == "done")
            return new SearchJobPollResult
            {
                IsProcessing = false,
                Result = new SearchJobResultDTO
                {
                    JobId        = jobId,
                    Status       = "done",
                    CompletedAt  = job.CompletedAt,
                    Items        = job.Result?.Items,
                    Page         = job.Result?.Page     ?? 1,
                    PageSize     = job.Result?.PageSize  ?? 20,
                    TotalCount   = job.Result?.TotalCount ?? 0
                }
            };

        // error
        return new SearchJobPollResult
        {
            IsProcessing = false,
            Result = new SearchJobResultDTO
            {
                JobId  = jobId,
                Status = "error",
                Error  = new SearchJobErrorDTO
                {
                    Code    = "SEARCH_FAILED",
                    Message = job.Error ?? "Search failed."
                }
            }
        };
    }

    private static async Task<PagedTripsDTO> ReadPagedTrips(NpgsqlCommand cmd, int page, int pageSize)
    {
        var items = new List<TripV1DTO>();
        var totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (items.Count == 0)
                totalCount = reader.GetInt32(reader.GetOrdinal("total_count"));
            items.Add(MapRowWithPassengers(reader));
        }

        return new PagedTripsDTO
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static TripV1DTO MapRowWithPassengers(NpgsqlDataReader r) =>
        MapRow(r, r.GetFieldValue<Guid[]>(r.GetOrdinal("passenger_ids"))
                  .Select(g => g.ToString())
                  .ToList());

    private static TripV1DTO MapRow(NpgsqlDataReader r, List<string> passengerIds) => new()
    {
        Id = r.GetGuid(r.GetOrdinal("id")).ToString(),
        DriverId = r.GetGuid(r.GetOrdinal("driver_user_id")).ToString(),
        Source = new LatLngDTO
        {
            Lat = r.GetDouble(r.GetOrdinal("source_lat")),
            Lng = r.GetDouble(r.GetOrdinal("source_lng"))
        },
        Target = new LatLngDTO
        {
            Lat = r.GetDouble(r.GetOrdinal("target_lat")),
            Lng = r.GetDouble(r.GetOrdinal("target_lng"))
        },
        RouteDistanceM = r.GetInt32(r.GetOrdinal("route_distance_m")),
        RouteDurationS = r.GetInt32(r.GetOrdinal("route_duration_s")),
        MaxDetourMeters = r.GetInt32(r.GetOrdinal("max_detour_m")),
        DepartureTime = r.GetDateTime(r.GetOrdinal("departure_time")),
        PricePerSeat = r.GetDecimal(r.GetOrdinal("price_per_seat")),
        AvailableSeats = r.GetInt16(r.GetOrdinal("available_seats")),
        Status = r.GetString(r.GetOrdinal("status")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
        PassengerIds = passengerIds
    };


}
