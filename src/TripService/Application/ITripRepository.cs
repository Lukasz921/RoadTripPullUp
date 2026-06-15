namespace TripService.Application;

public interface ITripRepository
{
    Task<TripDTO> InsertAsync(Guid driverId, CreateTripDTO dto, RouteResult route);
    Task<TripDTO?> FindByIdAsync(Guid id);
    Task<PagedTripsDTO> GetByDriverAsync(Guid driverId, int page, int pageSize);
    Task<PagedTripsDTO> GetByPassengerAsync(Guid userId, int page, int pageSize);
    Task<PagedTripsDTO> GetPastTripsAsync(Guid userId, int page, int pageSize);
    Task<PagedTripsDTO> GetAllAsync(DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
    Task<Guid?> GetDriverIdAsync(Guid tripId);
    Task DeleteAsync(Guid id);

    Task RateTripAsync(Guid tripId, Guid raterId, Guid ratedId, int rating);
    Task<bool> HasRatedAsync(Guid tripId, Guid raterId, Guid ratedId);
    Task<bool> IsPassengerAsync(Guid tripId, Guid userId);

    // Runs SELECT FOR UPDATE + INSERT in one transaction.
    // Pre-checks (UUID validity, user exists, driver != passenger) are the caller's responsibility.
    // Throws NotFoundException, ForbiddenException, ValidationException, SeatUnavailableException.
    Task AddPassengerTransactionalAsync(Guid tripId, Guid driverGuid, Guid passengerGuid);

    // --- Trip requests ---
    Task<TripRequestDTO> InsertTripRequestAsync(
        Guid tripId, Guid requesterId, Guid conversationId,
        LatLngDTO pickup, LatLngDTO dropoff, int detourM, RouteResult preview);
    Task<TripRequestDTO?> FindPendingRequestAsync(Guid tripId, Guid requesterId);
    Task<TripRequestDTO?> FindRequestByConversationAsync(Guid conversationId);
    Task<TripRequestDTO?> FindRequestByIdAsync(Guid requestId);
    // Ordered (by accepted_at) pickup/dropoff pairs of already-accepted requests, for route recompute.
    Task<List<(LatLngDTO Pickup, LatLngDTO Dropoff)>> GetAcceptedRequestStopsAsync(Guid tripId);
    // SELECT FOR UPDATE + validate + UPDATE trip route + INSERT passenger + mark request ACCEPTED, one tx.
    Task AcceptTripRequestTransactionalAsync(
        Guid tripId, Guid driverGuid, Guid requestId, Guid requesterGuid, RouteResult newRoute);
}
