using Application.DTOs;

namespace Application.Interfaces.Trip;

public interface ITripService
{
    Task<CreateTripResponseDTO> CreateTrip(CreateTripDTO dto, Guid driverId);

    Task<List<TripSummaryDTO>> SearchTrips(SearchTripsCriteria criteria);

    Task<TripDetailsDTO> GetById(Guid id);

    Task RequestRide(Guid tripId, Guid passengerId);

    Task AcceptRequest(Guid requestId, Guid driverId);
}
