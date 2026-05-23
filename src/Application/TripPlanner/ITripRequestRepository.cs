using Core.TripPlanner;

namespace Application.TripPlanner;

public interface ITripRequestRepository
{
    Task Save(TripRequest request);
    Task<TripRequest?> GetById(Guid id);
    Task<List<TripRequest>> GetByTripId(Guid tripId);
}
