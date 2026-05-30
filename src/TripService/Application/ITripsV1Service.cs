namespace TripService.Application;

public interface ITripsV1Service
{
    Task<TripV1DTO> CreateTripAsync(CreateTripV1DTO dto, string driverId);
    Task<TripV1DTO> GetTripAsync(string tripId);
    Task<PagedTripsDTO> GetMyTripsAsync(string driverId, int page, int pageSize);
    Task<PagedTripsDTO> GetMyPassengerTripsAsync(string userId, int page, int pageSize);
    Task JoinTripAsync(string tripId, string userId);
    Task DeleteTripAsync(string tripId, string driverId);
    Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsV1RequestDTO dto, string userId);
    Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId);
}
