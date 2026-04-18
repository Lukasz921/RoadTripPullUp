using Core.Entities;

namespace Application.Interfaces.Trip;

public interface ITripRequestRepository
{
    Task Save(TripRequest request);
    Task<TripRequest?> GetById(Guid id);
    Task<List<TripRequest>> GetByTripId(Guid tripId);
}
