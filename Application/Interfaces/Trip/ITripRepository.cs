using Application.DTOs;
using Core.Entities;

namespace Application.Interfaces.Trip;

using TripEntity = Core.Entities.Trip;

public interface ITripRepository
{
    Task Save(TripEntity trip);

    Task<List<TripEntity>> Search(SearchTripsCriteria criteria);

    Task<TripEntity?> GetById(Guid id);
}
