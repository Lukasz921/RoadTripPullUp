using Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public class TripsV1Service : ITripsV1Service
{
    private readonly string _connectionString;
    private readonly MockTripsV1Service _mock = new();

    public TripsV1Service(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("TripConnection")
            ?? throw new InvalidOperationException("TripConnection is not configured.");
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

        var distanceM = (int)HaversineMeters(dto.Source.Lat, dto.Source.Lng, dto.Target.Lat, dto.Target.Lng);
        var durationS = (int)(distanceM / 13.9); // ~50 km/h average

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
                ST_SetSRID(ST_MakeLine(ST_MakePoint(@srcLng, @srcLat), ST_MakePoint(@tgtLng, @tgtLat)), 4326)::geography,
                @distanceM,
                @durationS,
                @maxDetourM,
                @departureTime,
                @pricePerSeat,
                @availableSeats
            )
            RETURNING
                id,
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
        cmd.Parameters.AddWithValue("distanceM", distanceM);
        cmd.Parameters.AddWithValue("durationS", durationS);
        cmd.Parameters.AddWithValue("maxDetourM", dto.MaxDetourMeters);
        cmd.Parameters.AddWithValue("departureTime", DateTime.SpecifyKind(dto.DepartureTime.ToUniversalTime(), DateTimeKind.Utc));
        cmd.Parameters.AddWithValue("pricePerSeat", dto.PricePerSeat);
        cmd.Parameters.AddWithValue("availableSeats", (short)dto.AvailableSeats);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new TripV1DTO
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
            DriverId = driverId,
            Source = new LatLngDTO
            {
                Lat = reader.GetDouble(reader.GetOrdinal("source_lat")),
                Lng = reader.GetDouble(reader.GetOrdinal("source_lng"))
            },
            Target = new LatLngDTO
            {
                Lat = reader.GetDouble(reader.GetOrdinal("target_lat")),
                Lng = reader.GetDouble(reader.GetOrdinal("target_lng"))
            },
            RouteDistanceM = reader.GetInt32(reader.GetOrdinal("route_distance_m")),
            RouteDurationS = reader.GetInt32(reader.GetOrdinal("route_duration_s")),
            MaxDetourMeters = reader.GetInt32(reader.GetOrdinal("max_detour_m")),
            DepartureTime = reader.GetDateTime(reader.GetOrdinal("departure_time")),
            PricePerSeat = reader.GetDecimal(reader.GetOrdinal("price_per_seat")),
            AvailableSeats = reader.GetInt16(reader.GetOrdinal("available_seats")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    public Task<TripV1DTO> GetTripAsync(string tripId) =>
        _mock.GetTripAsync(tripId);

    public Task<MyTripsV1ResultDTO> GetMyTripsAsync(string driverId, string status, int limit) =>
        _mock.GetMyTripsAsync(driverId, status, limit);

    public Task DeleteTripAsync(string tripId, string driverId) =>
        _mock.DeleteTripAsync(tripId, driverId);

    public Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsV1RequestDTO dto, string userId) =>
        _mock.SubmitSearchAsync(dto, userId);

    public Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId) =>
        _mock.PollSearchJobAsync(jobId, userId);

    private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
