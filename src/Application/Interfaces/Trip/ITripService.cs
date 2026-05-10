using Application.DTOs;

namespace Application.Interfaces.Trip;

public interface ITripService
{
    Task<CreateTripResponseDTO> CreateTrip(CreateTripDTO dto, Guid driverId);

    Task<List<TripSummaryDTO>> SearchTrips(SearchTripsCriteria criteria);

    Task<TripDetailsDTO> GetById(Guid id);

<<<<<<< Updated upstream:src/Application/Interfaces/Trip/ITripService.cs
    Task RequestRide(Guid tripId, Guid passengerId);

    Task AcceptRequest(Guid requestId, Guid driverId);

    Task<List<TripRequestDTO>> GetRequestsForTrip(Guid tripId, Guid driverId);
=======
    Task<List<TripSummaryDTO>> GetMyTrips(Guid driverId);
>>>>>>> Stashed changes:Application/Interfaces/Trip/ITripService.cs
}
