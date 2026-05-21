using Core.Users;

namespace Application.Users;

public interface IUserRepository
{
    Task Save(User user);
    Task<User?> FindById(Guid id);
    Task<User?> FindByEmail(string email);
}
