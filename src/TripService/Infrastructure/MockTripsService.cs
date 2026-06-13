using Application.Exceptions;
using TripService.Application;
using System.Collections.Concurrent;

namespace TripService.Infrastructure;

public class MockTripsService : ITripsService
{
    private static readonly ConcurrentDictionary<string, TripData> _trips = new();
    private static readonly ConcurrentDictionary<string, SearchJob> _jobs = new();

    public Task<TripDTO> CreateTripAsync(CreateTripDTO dto, string driverId)
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
            SourceLat = dto.Source.Lat, SourceLng = dto.Source.Lng,
            TargetLat = dto.Target.Lat, TargetLng = dto.Target.Lng,
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

    public Task<TripDTO> GetTripAsync(string tripId)
    {
        if (!_trips.TryGetValue(tripId, out var trip))
            throw new NotFoundException($"Trip {tripId} not found.");
        return Task.FromResult(ToTripDTO(trip));
    }

    public Task<PagedTripsDTO> GetMyTripsAsync(string driverId, int page, int pageSize)
    {
        var all = _trips.Values
            .Where(t => t.DriverId == driverId && t.Status == "ACTIVE" && t.DepartureTime >= DateTime.UtcNow)
            .OrderBy(t => t.DepartureTime)
            .ToList();

        var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(ToTripDTO).ToList();
        return Task.FromResult(new PagedTripsDTO
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = all.Count
        });
    }

    public Task<PagedTripsDTO> GetMyPassengerTripsAsync(string userId, int page, int pageSize)
    {
        var all = _trips.Values
            .Where(t => t.Status == "ACTIVE" && t.PassengerIds.Contains(userId) && t.DepartureTime >= DateTime.UtcNow)
            .OrderBy(t => t.DepartureTime)
            .ToList();

        var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(ToTripDTO).ToList();
        return Task.FromResult(new PagedTripsDTO
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = all.Count
        });
    }

    public Task<PagedTripsDTO> GetMyPastTripsAsync(string userId, int page, int pageSize)
    {
        var all = _trips.Values
            .Where(t => t.DepartureTime < DateTime.UtcNow && (t.DriverId == userId || t.PassengerIds.Contains(userId)))
            .OrderByDescending(t => t.DepartureTime)
            .ToList();

        var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(ToTripDTO).ToList();
        return Task.FromResult(new PagedTripsDTO
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = all.Count
        });
    }

    public Task AddPassengerAsync(string tripId, string driverId, string passengerId)
    {
        if (!_trips.TryGetValue(tripId, out var trip))
            throw new NotFoundException($"Trip {tripId} not found.");
        if (trip.DriverId != driverId)
            throw new ForbiddenException("Only the trip driver can add passengers.");
        if (driverId == passengerId)
            throw new ValidationException("Driver cannot be added as a passenger on their own trip.");
        if (trip.Status != "ACTIVE")
            throw new ValidationException("Trip is not active.");
        if (trip.PassengerIds.Contains(passengerId))
            throw new ValidationException("This user is already a passenger on this trip.");
        if (trip.PassengerIds.Count >= trip.AvailableSeats)
            throw new SeatUnavailableException("No seats available on this trip.");

        trip.PassengerIds.Add(passengerId);
        return Task.CompletedTask;
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

    public Task<PagedTripsDTO> GetAllTripsAsync(DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        var all = _trips.Values
            .Where(t => dateFrom == null || t.DepartureTime >= dateFrom.Value)
            .Where(t => dateTo   == null || t.DepartureTime <= dateTo.Value)
            .OrderBy(t => t.DepartureTime)
            .ToList();

        var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(ToTripDTO).ToList();
        return Task.FromResult(new PagedTripsDTO
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = all.Count
        });
    }

    public Task AdminDeleteTripAsync(string tripId)
    {
        if (!_trips.TryGetValue(tripId, out _))
            throw new NotFoundException($"Trip {tripId} not found.");

        _trips.TryRemove(tripId, out _);
        return Task.CompletedTask;
    }

    public Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsRequestDTO dto, string userId)
    {
        if (!DateOnly.TryParse(dto.DateFrom, out var dateFrom))
            throw new ValidationException("dateFrom must be in YYYY-MM-DD format.");
        if (!DateOnly.TryParse(dto.DateTo, out var dateTo))
            throw new ValidationException("dateTo must be in YYYY-MM-DD format.");
        if (dateTo < dateFrom)
            throw new ValidationException("dateTo must be >= dateFrom.");

        var page = Math.Max(dto.Page, 1);
        var pageSize = Math.Clamp(dto.PageSize, 1, 100);
        var minSeats = dto.MinSeats <= 0 ? 1 : dto.MinSeats;
        var sortByPrice = dto.SortBy?.ToLowerInvariant() == "price";

        var fromUtc = dateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = dateTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var candidates = _trips.Values
            .Where(t => t.Status == "ACTIVE")
            .Where(t => t.DepartureTime >= fromUtc && t.DepartureTime <= toUtc)
            .Where(t => (t.AvailableSeats - t.PassengerIds.Count) >= minSeats)
            .Where(t => dto.MaxPrice == null || t.PricePerSeat <= dto.MaxPrice);

        var sorted = sortByPrice
            ? candidates.OrderBy(t => t.PricePerSeat)
            : candidates.OrderBy(t => t.DepartureTime);

        var all = sorted.ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => ToTripSummaryDTO(t, dto.Source))
            .ToList();

        var jobId = Guid.NewGuid().ToString("N");
        _jobs[jobId] = new SearchJob
        {
            JobId = jobId, UserId = userId, CreatedAt = DateTime.UtcNow,
            Items = items, Page = page, PageSize = pageSize, TotalCount = all.Count
        };

        return Task.FromResult(new SearchJobCreatedDTO
        {
            JobId = jobId,
            Status = "pending",
            StatusUrl = $"/api/v1/trips/search/{jobId}",
            EstimatedDurationMs = 2000
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
                    Status = elapsed.TotalSeconds < 0.5 ? "pending" : "processing",
                    Progress = new SearchProgressDetailsDTO
                    {
                        Phase = elapsed.TotalSeconds < 0.5 ? "queued" : "validating_routes",
                        CandidatesFound = job.Items.Count,
                        CandidatesProcessed = (int)(job.Items.Count * Math.Min(elapsed.TotalSeconds / 2.0, 1.0))
                    }
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

    private static TripDTO ToTripDTO(TripData t) => new()
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
        PassengerIds = new List<string>(t.PassengerIds),
        Status = t.Status,
        CreatedAt = t.CreatedAt
    };

    private static TripSummaryDTO ToTripSummaryDTO(TripData t, LatLngDTO passengerSource) => new()
    {
        Id = t.Id,
        DriverId = t.DriverId,
        Source = new LatLngDTO { Lat = t.SourceLat, Lng = t.SourceLng },
        Target = new LatLngDTO { Lat = t.TargetLat, Lng = t.TargetLng },
        DepartureTime = t.DepartureTime,
        PricePerSeat = t.PricePerSeat,
        AvailableSeats = t.AvailableSeats - t.PassengerIds.Count,
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
        public List<string> PassengerIds { get; set; } = new();
        public string Status { get; set; } = "ACTIVE";
        public DateTime CreatedAt { get; set; }
    }

    private class SearchJob
    {
        public string JobId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<TripSummaryDTO> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
