using Core.Entities;

namespace Application.Interfaces.Trip;

public interface IUserRepository
{
    Task Save(User user);
    Task<User?> FindById(Guid id);
    Task<User?> FindByEmail(string email);
}