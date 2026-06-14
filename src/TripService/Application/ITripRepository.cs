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
}
