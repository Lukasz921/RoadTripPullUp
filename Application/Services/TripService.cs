using Application.DTOs;
using Application.Interfaces.Trip;
using Core.Entities;
using Application.Exceptions;

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

        // Normalize date to UTC to avoid writing Local DateTime to timestamptz
        DateTime tripDateUtc;
        switch (dto.Date.Kind)
        {
            case DateTimeKind.Utc:
                tripDateUtc = dto.Date;
                break;
            case DateTimeKind.Local:
                tripDateUtc = dto.Date.ToUniversalTime();
                break;
            default:
                // Unspecified: assume input represents a UTC date/time (ISO with Z should be parsed as Utc by model binder),
                // but to be safe, treat as UTC without conversion.
                tripDateUtc = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
                break;
        }

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = driverId,
            RouteId = route.Id,
            Price = dto.Price,
            Date = tripDateUtc,
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

    public async Task<List<TripSummaryDTO>> SearchTrips(SearchTripsCriteria criteria)
    {
        // lightweight normalization
        if (criteria == null)
        {
            criteria = new SearchTripsCriteria();
        }

        if (!string.IsNullOrWhiteSpace(criteria.From))
        {
            criteria.From = criteria.From.Trim();
        }

        if (!string.IsNullOrWhiteSpace(criteria.To))
        {
            criteria.To = criteria.To.Trim();
        }

        var trips = await _tripRepository.Search(criteria);

        var results = new List<TripSummaryDTO>();

        foreach (var trip in trips)
        {
            // load route if necessary via route repository
            var route = await _routeRepository.GetById(trip.RouteId);

            results.Add(new TripSummaryDTO
            {
                TripId = trip.Id,
                DriverId = trip.DriverId,
                Price = (decimal)trip.Price,
                Date = trip.Date,
                MaxPassengers = trip.MaxPassengers,
                Route = new RouteDTO
                {
                    RouteId = route.Id,
                    From = route.From,
                    To = route.To
                }
            });
        }

        return results;
    }

    public async Task<TripDetailsDTO> GetById(Guid id)
    {
        var trip = await _tripRepository.GetById(id);
        if (trip == null)
            throw new NotFoundException($"Trip with id {id} not found.");

        var route = await _routeRepository.GetById(trip.RouteId);

        var dto = new TripDetailsDTO
        {
            TripId = trip.Id,
            DriverId = trip.DriverId,
            Price = (decimal)trip.Price,
            Date = trip.Date,
            MaxPassengers = trip.MaxPassengers,
            Status = Enum.Parse<TripStatus>(trip.OfferStatus.ToString()),
            Route = new RouteDTO
            {
                RouteId = route.Id,
                From = route.From,
                To = route.To
            },
            PassengerIds = trip.Passengers?.Select(p => p.Id).ToList() ?? new List<Guid>()
        };

        return dto;
    }

    private static void ValidateInput(CreateTripDTO dto, Guid driverId)
    {
        if (driverId == Guid.Empty)
        {
            throw new ValidationException("Invalid driver identifier.");
        }

        if (dto.Route == null)
        {
            throw new ValidationException("Route is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Route.From) || string.IsNullOrWhiteSpace(dto.Route.To))
        {
            throw new ValidationException("Route origin and destination are required.");
        }

        if (dto.Route.From.Trim().Equals(dto.Route.To.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Route origin and destination cannot be the same.");
        }

        if (dto.Price <= 0)
        {
            throw new ValidationException("Trip price must be greater than zero.");
        }

        if (dto.MaxPassengers <= 0)
        {
            throw new ValidationException("Max passengers must be greater than zero.");
        }

        // compare in UTC
        DateTime tripDateUtcForValidation;
        switch (dto.Date.Kind)
        {
            case DateTimeKind.Utc:
                tripDateUtcForValidation = dto.Date;
                break;
            case DateTimeKind.Local:
                tripDateUtcForValidation = dto.Date.ToUniversalTime();
                break;
            default:
                tripDateUtcForValidation = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
                break;
        }
        if (tripDateUtcForValidation <= DateTime.UtcNow)
        {
            throw new ValidationException("Trip date must be in the future.");
        }
    }
}
