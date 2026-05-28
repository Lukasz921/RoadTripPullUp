using Application.Exceptions;
using Application.TripPlanner;
using System.Collections.Concurrent;

namespace Infrastructure.TripPlanner;

public class MockTripsV1Service : ITripsV1Service
{
    private static readonly ConcurrentDictionary<string, TripData> _trips = new();
    private static readonly ConcurrentDictionary<string, SearchJob> _jobs = new();

    public Task<TripV1DTO> CreateTripAsync(CreateTripV1DTO dto, string driverId)
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

        var trip = new TripData
        {
            Id = Guid.NewGuid().ToString(),
            DriverId = driverId,
            SourceLat = dto.Source.Lat,
            SourceLng = dto.Source.Lng,
            TargetLat = dto.Target.Lat,
            TargetLng = dto.Target.Lng,
            DepartureTime = DateTime.SpecifyKind(dto.DepartureTime.ToUniversalTime(), DateTimeKind.Utc),
            RouteDistanceM = distanceM,
            RouteDurationS = (int)(distanceM / 25.0),
            MaxDetourMeters = dto.MaxDetourMeters,
            PricePerSeat = dto.PricePerSeat,
            AvailableSeats = dto.AvailableSeats,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _trips[trip.Id] = trip;
        return Task.FromResult(ToTripDTO(trip));
    }

    public Task<TripV1DTO> GetTripAsync(string tripId)
    {
        if (!_trips.TryGetValue(tripId, out var trip))
            throw new NotFoundException($"Trip {tripId} not found.");
        return Task.FromResult(ToTripDTO(trip));
    }

    public Task<MyTripsV1ResultDTO> GetMyTripsAsync(string driverId, int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var all = _trips.Values
            .Where(t => t.DriverId == driverId && t.Status == "ACTIVE")
            .OrderBy(t => t.DepartureTime)
            .ToList();

        var items = all
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(ToTripDTO)
            .ToList();

        return Task.FromResult(new MyTripsV1ResultDTO
        {
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = all.Count
        });
    }

    public Task DeleteTripAsync(string tripId, string driverId)
    {
        if (!_trips.TryGetValue(tripId, out var trip))
            throw new NotFoundException($"Trip {tripId} not found.");
        if (trip.DriverId != driverId)
            throw new ForbiddenException("You can only delete your own trips.");

        _trips.TryRemove(tripId, out _);
        return Task.CompletedTask;
    }

    public Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsV1RequestDTO dto, string userId)
    {
        if (!DateOnly.TryParse(dto.DateFrom, out var dateFrom))
            throw new ValidationException("dateFrom must be in YYYY-MM-DD format.");
        if (!DateOnly.TryParse(dto.DateTo, out var dateTo))
            throw new ValidationException("dateTo must be in YYYY-MM-DD format.");
        if (dateTo < dateFrom)
            throw new ValidationException("dateTo must be >= dateFrom.");

        var minSeats = dto.MinSeats <= 0 ? 1 : dto.MinSeats;
        var safePage = Math.Max(1, dto.Page);
        var safePageSize = Math.Clamp(dto.PageSize <= 0 ? 20 : dto.PageSize, 1, 100);
        var sortByPrice = dto.SortBy?.ToLowerInvariant() == "price";

        var fromUtc = dateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = dateTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var candidates = _trips.Values
            .Where(t => t.Status == "ACTIVE")
            .Where(t => t.DepartureTime >= fromUtc && t.DepartureTime <= toUtc)
            .Where(t => t.AvailableSeats >= minSeats)
            .Where(t => dto.MaxPrice == null || t.PricePerSeat <= dto.MaxPrice);

        var sorted = (sortByPrice
            ? candidates.OrderBy(t => t.PricePerSeat)
            : candidates.OrderBy(t => t.DepartureTime)).ToList();

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(t => ToTripSummaryDTO(t, dto.Source))
            .ToList();

        var jobId = Guid.NewGuid().ToString("N");
        _jobs[jobId] = new SearchJob
        {
            JobId = jobId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = totalCount
        };

        return Task.FromResult(new SearchJobCreatedDTO
        {
            JobId = jobId,
            Status = "processing",
            StatusUrl = $"/api/v1/trips/search/{jobId}",
            EstimatedDurationMs = 3000
        });
    }

    public Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId)
    {
        if (!_jobs.TryGetValue(jobId, out var job) || job.UserId != userId)
            throw new NotFoundException($"Search job {jobId} not found.");

        var elapsed = DateTime.UtcNow - job.CreatedAt;

        if (elapsed.TotalSeconds < 2)
        {
            return Task.FromResult(new SearchJobPollResult
            {
                IsProcessing = true,
                Progress = new SearchJobProgressDTO
                {
                    JobId = jobId,
                    Status = "processing"
                }
            });
        }

        return Task.FromResult(new SearchJobPollResult
        {
            IsProcessing = false,
            Result = new SearchJobResultDTO
            {
                JobId = jobId,
                Status = "done",
                CompletedAt = job.CreatedAt.AddSeconds(2),
                Items = job.Items,
                Page = job.Page,
                PageSize = job.PageSize,
                TotalCount = job.TotalCount
            }
        });
    }

    private static TripV1DTO ToTripDTO(TripData t) => new()
    {
        Id = t.Id,
        DriverId = t.DriverId,
        Source = new LatLngDTO { Lat = t.SourceLat, Lng = t.SourceLng },
        Target = new LatLngDTO { Lat = t.TargetLat, Lng = t.TargetLng },
        DepartureTime = t.DepartureTime,
        RouteDistanceM = t.RouteDistanceM,
        RouteDurationS = t.RouteDurationS,
        MaxDetourMeters = t.MaxDetourMeters,
        PricePerSeat = t.PricePerSeat,
        AvailableSeats = t.AvailableSeats,
        Status = t.Status,
        CreatedAt = t.CreatedAt
    };

    private static TripSummaryV1DTO ToTripSummaryDTO(TripData t, LatLngDTO passengerSource) => new()
    {
        Id = t.Id,
        DriverId = t.DriverId,
        Source = new LatLngDTO { Lat = t.SourceLat, Lng = t.SourceLng },
        Target = new LatLngDTO { Lat = t.TargetLat, Lng = t.TargetLng },
        DepartureTime = t.DepartureTime,
        PricePerSeat = t.PricePerSeat,
        AvailableSeats = t.AvailableSeats,
        MaxDetourMeters = t.MaxDetourMeters,
        ActualDetourMeters = Math.Min(
            (int)HaversineMeters(passengerSource.Lat, passengerSource.Lng, t.SourceLat, t.SourceLng),
            t.MaxDetourMeters)
    };

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

    private class TripData
    {
        public string Id { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public double SourceLat { get; set; }
        public double SourceLng { get; set; }
        public double TargetLat { get; set; }
        public double TargetLng { get; set; }
        public DateTime DepartureTime { get; set; }
        public int RouteDistanceM { get; set; }
        public int RouteDurationS { get; set; }
        public int MaxDetourMeters { get; set; }
        public decimal PricePerSeat { get; set; }
        public int AvailableSeats { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public DateTime CreatedAt { get; set; }
    }

    private class SearchJob
    {
        public string JobId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<TripSummaryV1DTO> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
