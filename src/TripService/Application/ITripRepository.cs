namespace TripService.Application;

public interface ITripRepository
{
    Task<TripV1DTO> InsertAsync(Guid driverId, CreateTripV1DTO dto, RouteResult route);
    Task<TripV1DTO?> FindByIdAsync(Guid id);
    Task<PagedTripsDTO> GetByDriverAsync(Guid driverId, int page, int pageSize);
    Task<PagedTripsDTO> GetByPassengerAsync(Guid userId, int page, int pageSize);
    Task<Guid?> GetDriverIdAsync(Guid tripId);
    Task DeleteAsync(Guid id);

    // Runs SELECT FOR UPDATE + INSERT in one transaction.
    // Pre-checks (UUID validity, user exists, driver != passenger) are the caller's responsibility.
    // Throws NotFoundException, ForbiddenException, ValidationException, SeatUnavailableException.
    Task AddPassengerTransactionalAsync(Guid tripId, Guid driverGuid, Guid passengerGuid);
}
