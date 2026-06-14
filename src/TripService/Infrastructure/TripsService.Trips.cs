using MessageService.Core.Exceptions;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsService
{
    public async Task<TripDTO> CreateTripAsync(CreateTripDTO dto, string driverId)
    {
        if (await _userChecker.IsUserBannedAsync(driverId))
            throw new ForbiddenException("Banned users cannot create trips.");

        if (dto.DepartureTime.ToUniversalTime() <= DateTime.UtcNow)
            throw new ValidationException("departureTime must be in the future.");
        if (dto.MaxDetourMeters <= 0)
            throw new ValidationException("maxDetourMeters must be positive.");
        if (dto.PricePerSeat <= 0)
            throw new ValidationException("pricePerSeat must be positive.");
        if (dto.AvailableSeats <= 0)
            throw new ValidationException("availableSeats must be positive.");

        var route = await _routing.GetRouteAsync(dto.Source, dto.Target);
        return await _repository.InsertAsync(Guid.Parse(driverId), dto, route);
    }

    public async Task<TripDTO> GetTripAsync(string tripId)
    {
        if (!Guid.TryParse(tripId, out var id))
            throw new NotFoundException($"Trip '{tripId}' not found.");

        var trip = await _repository.FindByIdAsync(id);
        if (trip is null)
            throw new NotFoundException($"Trip '{tripId}' not found.");

        return trip;
    }

    public async Task DeleteTripAsync(string tripId, string driverId)
    {
        if (!Guid.TryParse(tripId, out var id))
            throw new NotFoundException($"Trip '{tripId}' not found.");

        var ownerId = await _repository.GetDriverIdAsync(id);
        if (ownerId is null)
            throw new NotFoundException($"Trip '{tripId}' not found.");
        if (ownerId.ToString() != driverId)
            throw new ForbiddenException("You are not the driver of this trip.");

        await _repository.DeleteAsync(id);
    }

    public async Task AdminDeleteTripAsync(string tripId)
    {
        if (!Guid.TryParse(tripId, out var id))
            throw new NotFoundException($"Trip '{tripId}' not found.");

        var ownerId = await _repository.GetDriverIdAsync(id);
        if (ownerId is null)
            throw new NotFoundException($"Trip '{tripId}' not found.");

        await _repository.DeleteAsync(id);
    }
}

