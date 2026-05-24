using MessageService.Core.Models;

namespace MessageService.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
}

