using Core.Entities;

namespace Application.Interfaces;

public interface ITripRepository
{
    Task Save(Trip trip);
}
