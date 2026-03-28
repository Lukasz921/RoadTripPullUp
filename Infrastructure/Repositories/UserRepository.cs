using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task Save(User user)
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