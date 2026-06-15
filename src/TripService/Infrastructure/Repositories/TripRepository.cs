using TripService.Application.Exceptions;
using MessageService.Core.Exceptions;
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

    public async Task RateTripAsync(Guid tripId, Guid raterId, Guid ratedId, int rating)
    {
        const string sql = """
            INSERT INTO trip_rating (trip_id, rater_user_id, rated_user_id, rating)
            VALUES (@tripId, @raterId, @ratedId, @rating)
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tripId",  tripId);
        cmd.Parameters.AddWithValue("raterId", raterId);
        cmd.Parameters.AddWithValue("ratedId", ratedId);
        cmd.Parameters.AddWithValue("rating",  (short)rating);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasRatedAsync(Guid tripId, Guid raterId, Guid ratedId)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM trip_rating WHERE trip_id = @tripId AND rater_user_id = @raterId AND rated_user_id = @ratedId)";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tripId",  tripId);
        cmd.Parameters.AddWithValue("raterId", raterId);
        cmd.Parameters.AddWithValue("ratedId", ratedId);

        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
    }

    public async Task<bool> IsPassengerAsync(Guid tripId, Guid userId)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM trip_passenger WHERE trip_id = @tripId AND passenger_user_id = @userId)";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tripId", tripId);
        cmd.Parameters.AddWithValue("userId", userId);

        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
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

    // --- Trip requests ---

    // Shared projection that maps 1:1 onto TripMapper.MapTripRequest.
    private const string RequestSelect = """
        SELECT id, trip_id, requester_user_id, conversation_id,
               ST_Y(pickup_geog::geometry)  AS pickup_lat,  ST_X(pickup_geog::geometry)  AS pickup_lng,
               ST_Y(dropoff_geog::geometry) AS dropoff_lat, ST_X(dropoff_geog::geometry) AS dropoff_lng,
               detour_m, status::text AS status,
               ST_AsGeoJSON(preview_polyline)::text AS preview_geojson
        FROM trip_request
        """;

    public async Task<TripRequestDTO> InsertTripRequestAsync(
        Guid tripId, Guid requesterId, Guid conversationId,
        LatLngDTO pickup, LatLngDTO dropoff, int detourM, RouteResult preview)
    {
        const string sql = """
            INSERT INTO trip_request (
                trip_id, requester_user_id, conversation_id,
                pickup_geog, dropoff_geog, preview_polyline, detour_m
            )
            VALUES (
                @tripId, @requesterId, @conversationId,
                ST_SetSRID(ST_MakePoint(@puLng, @puLat), 4326)::geography,
                ST_SetSRID(ST_MakePoint(@doLng, @doLat), 4326)::geography,
                ST_GeomFromText(@previewWkt, 4326)::geography,
                @detourM
            )
            RETURNING
                id, trip_id, requester_user_id, conversation_id,
                ST_Y(pickup_geog::geometry)  AS pickup_lat,  ST_X(pickup_geog::geometry)  AS pickup_lng,
                ST_Y(dropoff_geog::geometry) AS dropoff_lat, ST_X(dropoff_geog::geometry) AS dropoff_lng,
                detour_m, status::text AS status,
                ST_AsGeoJSON(preview_polyline)::text AS preview_geojson
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tripId",         tripId);
        cmd.Parameters.AddWithValue("requesterId",    requesterId);
        cmd.Parameters.AddWithValue("conversationId", conversationId);
        cmd.Parameters.AddWithValue("puLat",          pickup.Lat);
        cmd.Parameters.AddWithValue("puLng",          pickup.Lng);
        cmd.Parameters.AddWithValue("doLat",          dropoff.Lat);
        cmd.Parameters.AddWithValue("doLng",          dropoff.Lng);
        cmd.Parameters.AddWithValue("previewWkt",     preview.PolylineWkt);
        cmd.Parameters.AddWithValue("detourM",        detourM);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return TripMapper.MapTripRequest(reader);
    }

    public async Task<TripRequestDTO?> FindPendingRequestAsync(Guid tripId, Guid requesterId)
    {
        var sql = RequestSelect + " WHERE trip_id = @tripId AND requester_user_id = @requesterId AND status = 'PENDING' LIMIT 1";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tripId",      tripId);
        cmd.Parameters.AddWithValue("requesterId", requesterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return TripMapper.MapTripRequest(reader);
    }

    public async Task<TripRequestDTO?> FindRequestByConversationAsync(Guid conversationId)
    {
        var sql = RequestSelect + " WHERE conversation_id = @conversationId ORDER BY created_at DESC LIMIT 1";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("conversationId", conversationId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return TripMapper.MapTripRequest(reader);
    }

    public async Task<TripRequestDTO?> FindRequestByIdAsync(Guid requestId)
    {
        var sql = RequestSelect + " WHERE id = @id";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", requestId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return TripMapper.MapTripRequest(reader);
    }

    public async Task<List<(LatLngDTO Pickup, LatLngDTO Dropoff)>> GetAcceptedRequestStopsAsync(Guid tripId)
    {
        const string sql = """
            SELECT ST_Y(pickup_geog::geometry)  AS pickup_lat,  ST_X(pickup_geog::geometry)  AS pickup_lng,
                   ST_Y(dropoff_geog::geometry) AS dropoff_lat, ST_X(dropoff_geog::geometry) AS dropoff_lng
            FROM trip_request
            WHERE trip_id = @tripId AND status = 'ACCEPTED'
            ORDER BY accepted_at
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tripId", tripId);

        var stops = new List<(LatLngDTO, LatLngDTO)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var pickup  = new LatLngDTO { Lat = reader.GetDouble(reader.GetOrdinal("pickup_lat")),  Lng = reader.GetDouble(reader.GetOrdinal("pickup_lng")) };
            var dropoff = new LatLngDTO { Lat = reader.GetDouble(reader.GetOrdinal("dropoff_lat")), Lng = reader.GetDouble(reader.GetOrdinal("dropoff_lng")) };
            stops.Add((pickup, dropoff));
        }
        return stops;
    }

    public async Task AcceptTripRequestTransactionalAsync(
        Guid tripId, Guid driverGuid, Guid requestId, Guid requesterGuid, RouteResult newRoute)
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
                EXISTS(SELECT 1 FROM trip_passenger WHERE trip_id = t.id AND passenger_user_id = @requesterId) AS already_joined,
                (SELECT status::text FROM trip_request WHERE id = @requestId AND trip_id = t.id) AS request_status
            FROM trip t
            WHERE t.id = @id
            FOR UPDATE
            """;

        await using var checkCmd = new NpgsqlCommand(checkSql, conn, tx);
        checkCmd.Parameters.AddWithValue("id",          tripId);
        checkCmd.Parameters.AddWithValue("requesterId", requesterGuid);
        checkCmd.Parameters.AddWithValue("requestId",   requestId);

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
        var requestStatusOrd = r.GetOrdinal("request_status");
        var requestStatus  = r.IsDBNull(requestStatusOrd) ? null : r.GetString(requestStatusOrd);
        await r.CloseAsync();

        if (tripDriverId != driverGuid)       throw new ForbiddenException("Only the trip driver can accept requests.");
        if (requestStatus is null)            throw new NotFoundException($"Request '{requestId}' not found.");
        if (requestStatus != "PENDING")       throw new ValidationException("This request is no longer pending.");
        if (status != "ACTIVE")               throw new ValidationException("Trip is not active.");
        if (alreadyJoined)                    throw new ValidationException("This user is already a passenger on this trip.");
        if (passengerCount >= availableSeats) throw new SeatUnavailableException("No seats available on this trip.");

        await using var routeCmd = new NpgsqlCommand("""
            UPDATE trip
            SET route_polyline   = ST_GeomFromText(@polylineWkt, 4326)::geography,
                route_distance_m = @distanceM,
                route_duration_s = @durationS
            WHERE id = @id
            """, conn, tx);
        routeCmd.Parameters.AddWithValue("id",          tripId);
        routeCmd.Parameters.AddWithValue("polylineWkt", newRoute.PolylineWkt);
        routeCmd.Parameters.AddWithValue("distanceM",   newRoute.DistanceM);
        routeCmd.Parameters.AddWithValue("durationS",   newRoute.DurationS);
        await routeCmd.ExecuteNonQueryAsync();

        await using var insertCmd = new NpgsqlCommand(
            "INSERT INTO trip_passenger (trip_id, passenger_user_id) VALUES (@id, @passengerId)",
            conn, tx);
        insertCmd.Parameters.AddWithValue("id",          tripId);
        insertCmd.Parameters.AddWithValue("passengerId", requesterGuid);
        await insertCmd.ExecuteNonQueryAsync();

        await using var reqCmd = new NpgsqlCommand(
            "UPDATE trip_request SET status = 'ACCEPTED', accepted_at = NOW() WHERE id = @requestId",
            conn, tx);
        reqCmd.Parameters.AddWithValue("requestId", requestId);
        await reqCmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }
}

