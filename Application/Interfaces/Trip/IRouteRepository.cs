using Core.Entities;

namespace Application.Interfaces.Trip;

public interface IRouteRepository
{
    Task Save(Route route);
    Task<Route> GetById(Guid id);
}
