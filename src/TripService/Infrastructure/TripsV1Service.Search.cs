using Application.Exceptions;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsV1Service
{
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
        cmd.Parameters.AddWithValue("offset",   (page - 1) * pageSize);

        return await TripV1Mapper.ReadPagedAsync(cmd, page, pageSize);
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
        cmd.Parameters.AddWithValue("userId",   userGuid);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset",   (page - 1) * pageSize);

        return await TripV1Mapper.ReadPagedAsync(cmd, page, pageSize);
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
            JobId               = jobId,
            Status              = "pending",
            StatusUrl           = $"/api/v1/trips/search/{jobId}",
            EstimatedDurationMs = 3_000
        };
    }

    public async Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId)
    {
        var job = await _jobStore.GetJobAsync(jobId, userId);

        if (job == null)
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
                    JobId       = jobId,
                    Status      = "done",
                    CompletedAt = job.CompletedAt,
                    Items       = job.Result?.Items,
                    Page        = job.Result?.Page      ?? 1,
                    PageSize    = job.Result?.PageSize   ?? 20,
                    TotalCount  = job.Result?.TotalCount ?? 0
                }
            };

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
}
