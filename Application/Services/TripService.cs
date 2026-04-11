using Application.DTOs;
using Application.Interfaces;
using Core.Entities;

namespace Application.Services;

public class TripService : ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly IRouteRepository _routeRepository;

    public TripService(ITripRepository tripRepository, IRouteRepository routeRepository)
    {
        _tripRepository = tripRepository;
        _routeRepository = routeRepository;
    }

    public async Task<CreateTripResponseDTO> CreateTrip(CreateTripDTO dto, Guid driverId)
    {
        ValidateInput(dto, driverId);

        var route = new Route
        {
            Id = Guid.NewGuid(),
            From = dto.Route.From.Trim(),
            To = dto.Route.To.Trim(),
            BetweenPoints = dto.Route.BetweenPoints
                .Where(point => !string.IsNullOrWhiteSpace(point))
                .Select(point => point.Trim())
                .ToList()
        };

        await _routeRepository.Save(route);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = driverId,
            RouteId = route.Id,
            Price = dto.Price,
            Date = dto.Date,
            MaxPassengers = dto.MaxPassengers,
            OfferStatus = TripStatus.Active
        };

        await _tripRepository.Save(trip);

        return new CreateTripResponseDTO
        {
            TripId = trip.Id,
            RouteId = route.Id,
            Price = trip.Price,
            Date = trip.Date,
            MaxPassengers = trip.MaxPassengers
        };
    }

    private static void ValidateInput(CreateTripDTO dto, Guid driverId)
    {
        if (driverId == Guid.Empty)
        {
            throw new Exception("Invalid driver identifier.");
        }

        if (dto.Route == null)
        {
            throw new Exception("Route is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Route.From) || string.IsNullOrWhiteSpace(dto.Route.To))
        {
            throw new Exception("Route origin and destination are required.");
        }

        if (dto.Route.From.Trim().Equals(dto.Route.To.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Route origin and destination cannot be the same.");
        }

        if (dto.Price <= 0)
        {
            throw new Exception("Trip price must be greater than zero.");
        }

        if (dto.MaxPassengers <= 0)
        {
            throw new Exception("Max passengers must be greater than zero.");
        }

        if (dto.Date <= DateTime.UtcNow)
        {
            throw new Exception("Trip date must be in the future.");
        }
    }
}
