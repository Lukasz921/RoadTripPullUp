namespace TripService.Application;

public interface ITripRepository
{
    Task<TripDTO> InsertAsync(Guid driverId, CreateTripDTO dto, RouteResult route);
    Task<TripDTO?> FindByIdAsync(Guid id);
    Task<PagedTripsDTO> GetByDriverAsync(Guid driverId, int page, int pageSize);
    Task<PagedTripsDTO> GetByPassengerAsync(Guid userId, int page, int pageSize);
    Task<Guid?> GetDriverIdAsync(Guid tripId);
    Task DeleteAsync(Guid id);

    // Runs SELECT FOR UPDATE + INSERT in one transaction.
    // Pre-checks (UUID validity, user exists, driver != passenger) are the caller's responsibility.
    // Throws NotFoundException, ForbiddenException, ValidationException, SeatUnavailableException.
    Task AddPassengerTransactionalAsync(Guid tripId, Guid driverGuid, Guid passengerGuid);
}
