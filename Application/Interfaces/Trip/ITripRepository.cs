using Application.DTOs;

namespace Application.Interfaces.Trip;

using Trip = Core.Entities.Trip;

public interface ITripRepository
{
    Task Save(Trip trip);

    Task<List<Trip>> Search(SearchTripsCriteria criteria);
}
