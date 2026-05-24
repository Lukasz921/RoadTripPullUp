using MessageService.Core.Models;

namespace MessageService.Core.RepositoryInterfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
}

