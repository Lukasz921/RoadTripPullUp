using Application.DTOs;

namespace Application.Interfaces;

public interface ITripService
{
    Task<CreateTripResponseDTO> CreateTrip(CreateTripDTO dto, Guid driverId);
}
