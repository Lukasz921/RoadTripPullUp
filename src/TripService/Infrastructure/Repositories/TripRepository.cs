using Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly string _connectionString;

    public TripRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("TripConnection")
            ?? throw new InvalidOperationException("TripConnection is not configured.");
    }

    public async Task<TripDTO> InsertAsync(Guid driverId, CreateTripDTO dto, RouteResult route)
    {
        const string sql = """
            INSERT INTO trip (
                driver_user_id, source_geog, target_geog, route_polyline,
                route_distance_m, route_duration_s, max_detour_m,
                departure_time, price_per_seat, available_seats
            )
            VALUES (
                @driverId,
                ST_SetSRID(ST_MakePoint(@srcLng, @srcLat), 4326)::geography,
                ST_SetSRID(ST_MakePoint(@tgtLng, @tgtLat), 4326)::geography,
                ST_GeomFromText(@polylineWkt, 4326)::geography,
                @distanceM, @durationS, @maxDetourM,
                @departureTime, @pricePerSeat, @availableSeats
            )
            RETURNING
                id, driver_user_id,
                ST_Y(source_geog::geometry) AS source_lat,
                ST_X(source_geog::geometry) AS source_lng,
                ST_Y(target_geog::geometry) AS target_lat,
                ST_X(target_geog::geometry) AS target_lng,
                route_distance_m, route_duration_s, max_detour_m,
                departure_time, price_per_seat, available_seats,
                status::text AS status, created_at
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("driverId",      driverId);
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
        return TripMapper.MapRow(reader, passengerIds: new());
    }

    public async Task<TripDTO?> FindByIdAsync(Guid id)
    {
        const string sql = """
            SELECT
                t.id, t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m, t.route_duration_s, t.max_detour_m,
                t.departure_time, t.price_per_seat, t.available_seats,
                t.status::text AS status, t.created_at,
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
        if (!await reader.ReadAsync()) return null;
        return TripMapper.MapRowDetail(reader);
    }

    public async Task<PagedTripsDTO> GetByDriverAsync(Guid driverId, int page, int pageSize)
    {
        const string sql = """
            SELECT
                t.id, t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m, t.route_duration_s, t.max_detour_m,
                t.departure_time, t.price_per_seat, t.available_seats,
                t.status::text AS status, t.created_at,
                COALESCE(
                    ARRAY_AGG(tp.passenger_user_id ORDER BY tp.joined_at) FILTER (WHERE tp.passenger_user_id IS NOT NULL),
                    '{}'::uuid[]
                ) AS passenger_ids,
                COUNT(*) OVER() AS total_count
            FROM trip t
            LEFT JOIN trip_passenger tp ON tp.trip_id = t.id
            WHERE t.driver_user_id = @driverId AND t.status = 'ACTIVE' AND t.departure_time >= NOW()
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
        cmd.Parameters.AddWithValue("driverId", driverId);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset",   (page - 1) * pageSize);
        return await TripMapper.ReadPagedAsync(cmd, page, pageSize);
    }

    public async Task<PagedTripsDTO> GetByPassengerAsync(Guid userId, int page, int pageSize)
    {
        const string sql = """
            SELECT
                t.id, t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m, t.route_duration_s, t.max_detour_m,
                t.departure_time, t.price_per_seat, t.available_seats,
                t.status::text AS status, t.created_at,
                COALESCE(
                    ARRAY_AGG(tp_all.passenger_user_id ORDER BY tp_all.joined_at) FILTER (WHERE tp_all.passenger_user_id IS NOT NULL),
                    '{}'::uuid[]
                ) AS passenger_ids,
                COUNT(*) OVER() AS total_count
            FROM trip t
            INNER JOIN trip_passenger tp_me ON tp_me.trip_id = t.id AND tp_me.passenger_user_id = @userId
            LEFT JOIN trip_passenger tp_all ON tp_all.trip_id = t.id
            WHERE t.status = 'ACTIVE' AND t.departure_time >= NOW()
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
        cmd.Parameters.AddWithValue("userId",   userId);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset",   (page - 1) * pageSize);
        return await TripMapper.ReadPagedAsync(cmd, page, pageSize);
    }

    public async Task<PagedTripsDTO> GetPastTripsAsync(Guid userId, int page, int pageSize)
    {
        const string sql = """
            SELECT
                t.id, t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m, t.route_duration_s, t.max_detour_m,
                t.departure_time, t.price_per_seat, t.available_seats,
                t.status::text AS status, t.created_at,
                COALESCE(
                    ARRAY_AGG(tp.passenger_user_id ORDER BY tp.joined_at) FILTER (WHERE tp.passenger_user_id IS NOT NULL),
                    '{}'::uuid[]
                ) AS passenger_ids,
                COUNT(*) OVER() AS total_count
            FROM trip t
            LEFT JOIN trip_passenger tp ON tp.trip_id = t.id
            WHERE t.departure_time < NOW()
              AND (
                  t.driver_user_id = @userId
                  OR EXISTS (SELECT 1 FROM trip_passenger WHERE trip_id = t.id AND passenger_user_id = @userId)
              )
            GROUP BY t.id, t.driver_user_id, t.source_geog, t.target_geog,
                     t.route_distance_m, t.route_duration_s, t.max_detour_m,
                     t.departure_time, t.price_per_seat, t.available_seats,
                     t.status, t.created_at
            ORDER BY t.departure_time DESC
            LIMIT @pageSize OFFSET @offset
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId",   userId);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset",   (page - 1) * pageSize);
        return await TripMapper.ReadPagedAsync(cmd, page, pageSize);
    }

    public async Task<PagedTripsDTO> GetAllAsync(DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        const string sql = """
            SELECT
                t.id, t.driver_user_id,
                ST_Y(t.source_geog::geometry) AS source_lat,
                ST_X(t.source_geog::geometry) AS source_lng,
                ST_Y(t.target_geog::geometry) AS target_lat,
                ST_X(t.target_geog::geometry) AS target_lng,
                t.route_distance_m, t.route_duration_s, t.max_detour_m,
                t.departure_time, t.price_per_seat, t.available_seats,
                t.status::text AS status, t.created_at,
                COALESCE(
                    ARRAY_AGG(tp.passenger_user_id ORDER BY tp.joined_at) FILTER (WHERE tp.passenger_user_id IS NOT NULL),
                    '{}'::uuid[]
                ) AS passenger_ids,
                COUNT(*) OVER() AS total_count
            FROM trip t
            LEFT JOIN trip_passenger tp ON tp.trip_id = t.id
            WHERE (@dateFrom IS NULL OR t.departure_time >= @dateFrom)
              AND (@dateTo   IS NULL OR t.departure_time <= @dateTo)
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

        cmd.Parameters.AddWithValue("dateFrom", (object?)dateFrom?.ToUniversalTime() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dateTo",   (object?)dateTo?.ToUniversalTime()   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset",   (page - 1) * pageSize);
        return await TripMapper.ReadPagedAsync(cmd, page, pageSize);
    }

    public async Task<Guid?> GetDriverIdAsync(Guid tripId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT driver_user_id FROM trip WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", tripId);
        var result = await cmd.ExecuteScalarAsync();
        return result is Guid g ? g : null;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM trip WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddPassengerTransactionalAsync(Guid tripId, Guid driverGuid, Guid passengerGuid)
    {
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
        checkCmd.Parameters.AddWithValue("id",          tripId);
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

        if (tripDriverId != driverGuid)     throw new ForbiddenException("Only the trip driver can add passengers.");
        if (status != "ACTIVE")             throw new ValidationException("Trip is not active.");
        if (alreadyJoined)                  throw new ValidationException("This user is already a passenger on this trip.");
        if (passengerCount >= availableSeats) throw new SeatUnavailableException("No seats available on this trip.");

        await using var insertCmd = new NpgsqlCommand(
            "INSERT INTO trip_passenger (trip_id, passenger_user_id) VALUES (@id, @passengerId)",
            conn, tx);
        insertCmd.Parameters.AddWithValue("id",          tripId);
        insertCmd.Parameters.AddWithValue("passengerId", passengerGuid);
        await insertCmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }
}
