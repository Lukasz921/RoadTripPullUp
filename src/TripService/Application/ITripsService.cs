namespace TripService.Application;

public interface ITripsService
{
    Task<TripDTO> CreateTripAsync(CreateTripDTO dto, string driverId);
    Task<TripDTO> GetTripAsync(string tripId);
    Task<PagedTripsDTO> GetMyTripsAsync(string driverId, int page, int pageSize);
    Task<PagedTripsDTO> GetMyPassengerTripsAsync(string userId, int page, int pageSize);
    Task AddPassengerAsync(string tripId, string driverId, string passengerId);
    Task DeleteTripAsync(string tripId, string driverId);
    Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsRequestDTO dto, string userId);
    Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId);
}
