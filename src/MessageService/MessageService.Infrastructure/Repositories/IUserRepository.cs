using MessageService.Models;

namespace MessageService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
}

