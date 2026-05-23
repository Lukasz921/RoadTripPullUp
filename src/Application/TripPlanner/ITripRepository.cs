using Core.TripPlanner;

namespace Application.TripPlanner;

using TripEntity = Core.TripPlanner.Trip;

public interface ITripRepository
{
    Task Save(TripEntity trip);
    Task<List<TripEntity>> Search(SearchTripsCriteria criteria);
    Task<TripEntity?> GetById(Guid id);
    Task<List<TripEntity>> GetByDriverIdAsync(Guid driverId);
}
