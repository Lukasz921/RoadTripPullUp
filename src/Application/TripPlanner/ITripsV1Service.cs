namespace Application.TripPlanner;

public interface ITripsV1Service
{
    Task<TripV1DTO> CreateTripAsync(CreateTripV1DTO dto, string driverId);
    Task<TripV1DTO> GetTripAsync(string tripId);
    Task<MyTripsV1ResultDTO> GetMyTripsAsync(string driverId, int page, int pageSize);
    Task DeleteTripAsync(string tripId, string driverId);
    Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsV1RequestDTO dto, string userId);
    Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId);
}
