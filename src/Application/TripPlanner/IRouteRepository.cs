using Core.TripPlanner;

namespace Application.TripPlanner;

public interface IRouteRepository
{
    Task Save(Route route);
    Task<Route> GetById(Guid id);
}
