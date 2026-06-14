using MessageService.Core.Exceptions;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsService
{
    public async Task AddPassengerAsync(string tripId, string driverId, string passengerId)
    {
        if (!Guid.TryParse(tripId, out var tripGuid))
            throw new NotFoundException($"Trip '{tripId}' not found.");
        if (!Guid.TryParse(driverId, out var driverGuid))
            throw new ForbiddenException("Invalid driver identity.");
        if (!Guid.TryParse(passengerId, out var passengerGuid))
            throw new ValidationException("passengerId is not a valid UUID.");
        if (driverGuid == passengerGuid)
            throw new ValidationException("Driver cannot be added as a passenger on their own trip.");

        if (!await _userChecker.UserExistsAsync(passengerId))
            throw new ValidationException($"User '{passengerId}' does not exist.");

        if (await _userChecker.IsUserBannedAsync(passengerId))
            throw new ForbiddenException("Banned users cannot join trips.");

        await _repository.AddPassengerTransactionalAsync(tripGuid, driverGuid, passengerGuid);
    }
}

