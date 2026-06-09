using Application.Exceptions;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsV1Service
{
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

        cmd.Parameters.AddWithValue("driverId",      Guid.Parse(driverId));
        cmd.Parameters.AddWithValue("srcLat",         dto.Source.Lat);
        cmd.Parameters.AddWithValue("srcLng",         dto.Source.Lng);
        cmd.Parameters.AddWithValue("tgtLat",         dto.Target.Lat);
        cmd.Parameters.AddWithValue("tgtLng",         dto.Target.Lng);
        cmd.Parameters.AddWithValue("polylineWkt",    route.PolylineWkt);
        cmd.Parameters.AddWithValue("distanceM",      route.DistanceM);
        cmd.Parameters.AddWithValue("durationS",      route.DurationS);
        cmd.Parameters.AddWithValue("maxDetourM",     dto.MaxDetourMeters);
        cmd.Parameters.AddWithValue("departureTime",  DateTime.SpecifyKind(dto.DepartureTime.ToUniversalTime(), DateTimeKind.Utc));
        cmd.Parameters.AddWithValue("pricePerSeat",   dto.PricePerSeat);
        cmd.Parameters.AddWithValue("availableSeats", (short)dto.AvailableSeats);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return TripV1Mapper.MapRow(reader, passengerIds: new());
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
                ST_AsGeoJSON(t.route_polyline)::text AS route_geojson,
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
                     t.status, t.created_at, t.route_polyline
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new NotFoundException($"Trip '{tripId}' not found.");

        return TripV1Mapper.MapRowDetail(reader);
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
}
