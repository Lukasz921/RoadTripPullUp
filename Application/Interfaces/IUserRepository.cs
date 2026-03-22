using Core.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task Save(User user);
    Task<User?> FindById(Guid id);
}