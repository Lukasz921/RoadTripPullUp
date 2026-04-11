using Core.Entities;

namespace Application.Interfaces;

public interface IRouteRepository
{
    Task Save(Route route);
}
