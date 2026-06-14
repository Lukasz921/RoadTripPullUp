using Application.Exceptions;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsService
{
    public async Task RateTripAsync(string tripId, string raterId, RateTripDTO dto)
    {
        if (!Guid.TryParse(tripId, out var tripGuid))
            throw new InvalidParametersException("Invalid trip ID format.");
        if (!Guid.TryParse(raterId, out var raterGuid))
            throw new InvalidParametersException("Invalid rater ID format.");

        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ValidationException("Rating must be between 1 and 5.");

        var driverGuid = await _repository.GetDriverIdAsync(tripGuid);
        if (driverGuid == null)
            throw new NotFoundException($"Trip '{tripId}' not found.");

        if (driverGuid == raterGuid)
            throw new ValidationException("Drivers cannot rate their own trips.");

        var isPassenger = await _repository.IsPassengerAsync(tripGuid, raterGuid);
        if (!isPassenger)
            throw new ForbiddenException("Only passengers can rate the trip.");

        var alreadyRated = await _repository.HasRatedAsync(tripGuid, raterGuid, driverGuid.Value);
        if (alreadyRated)
            throw new ValidationException("You have already rated this trip.");

        await _repository.RateTripAsync(tripGuid, raterGuid, driverGuid.Value, dto.Rating);
    }
}
