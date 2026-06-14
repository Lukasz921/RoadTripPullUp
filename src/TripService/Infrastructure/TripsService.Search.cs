using MessageService.Core.Exceptions;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsService
{
    public async Task<PagedTripsDTO> GetMyTripsAsync(string driverId, int page, int pageSize)
    {
        if (!Guid.TryParse(driverId, out var driverGuid))
            return new PagedTripsDTO { Page = page, PageSize = pageSize };

        return await _repository.GetByDriverAsync(driverGuid, page, pageSize);
    }

    public async Task<PagedTripsDTO> GetMyPassengerTripsAsync(string userId, int page, int pageSize)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return new PagedTripsDTO { Page = page, PageSize = pageSize };

        return await _repository.GetByPassengerAsync(userGuid, page, pageSize);
    }

    public async Task<PagedTripsDTO> GetMyPastTripsAsync(string userId, int page, int pageSize)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return new PagedTripsDTO { Page = page, PageSize = pageSize };

        return await _repository.GetPastTripsAsync(userGuid, page, pageSize);
    }

    public Task<PagedTripsDTO> GetAllTripsAsync(DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
        => _repository.GetAllAsync(dateFrom, dateTo, page, pageSize);

    public async Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsRequestDTO dto, string userId)
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

