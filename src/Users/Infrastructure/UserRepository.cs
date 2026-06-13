using Users.Application.Interfaces;
using Users.Core;
using Microsoft.EntityFrameworkCore;

namespace Users.Infrastructure;

public class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task Save(User user)
    {
        var trackedEntity = _context.Users.Local.FirstOrDefault(u => u.Id == user.Id);
        
        if (trackedEntity == null)
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == user.Id);
            if (!exists)
            {
                _context.Users.Add(user);
            }
            else
            {
                _context.Users.Update(user);
            }
        }
        // If it is already tracked, EF Core will detect changes automatically 
        // when SaveChangesAsync is called, so we don't need to do anything.

        await _context.SaveChangesAsync();
    }

    public async Task<User?> FindById(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> FindByEmail(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}
