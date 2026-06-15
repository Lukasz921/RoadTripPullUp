namespace TripService.Application;

public interface ITripsService
{
    Task<TripDTO> CreateTripAsync(CreateTripDTO dto, string driverId);
    Task<TripDTO> GetTripAsync(string tripId);
    Task<PagedTripsDTO> GetMyTripsAsync(string driverId, int page, int pageSize);
    Task<PagedTripsDTO> GetMyPassengerTripsAsync(string userId, int page, int pageSize);
    Task<PagedTripsDTO> GetMyPastTripsAsync(string userId, int page, int pageSize);
    Task<PagedTripsDTO> GetAllTripsAsync(DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
    Task AdminDeleteTripAsync(string tripId);
    Task AddPassengerAsync(string tripId, string driverId, string passengerId);
    Task<TripRequestDTO> CreateTripRequestAsync(string tripId, string requesterId, Guid conversationId, LatLngDTO pickup, LatLngDTO dropoff);
    Task<TripRequestDTO?> GetPendingTripRequestAsync(string tripId, string requesterId);
    Task<TripRequestDTO?> GetTripRequestByConversationAsync(string conversationId);
    // Recomputes the trip route through the new stops and adds the passenger; returns the requester id.
    Task<string> AcceptTripRequestAsync(string tripId, string driverId, string requestId);
    Task DeleteTripAsync(string tripId, string driverId);
    Task RateTripAsync(string tripId, string raterId, RateTripDTO dto);
    Task<SearchJobCreatedDTO> SubmitSearchAsync(SearchTripsRequestDTO dto, string userId);
    Task<SearchJobPollResult> PollSearchJobAsync(string jobId, string userId);
}
