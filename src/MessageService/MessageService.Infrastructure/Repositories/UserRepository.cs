using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace MessageService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }
}

