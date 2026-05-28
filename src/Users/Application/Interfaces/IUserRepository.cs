using Users.Core;

namespace Users.Application.Interfaces;

public interface IUserRepository
{
    Task Save(User user);
    Task<User?> FindById(Guid id);
    Task<User?> FindByEmail(string email);
}
