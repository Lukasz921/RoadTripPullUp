using Microsoft.EntityFrameworkCore;
using Users.Application.Interfaces;
using Users.Core;

namespace Users.Infrastructure;

public class RatingRepository : IRatingRepository
{
    private readonly UsersDbContext _dbContext;

    public RatingRepository(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Rating rating)
    {
        await _dbContext.Ratings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<Rating>> GetByUserId(Guid userId)
    {
        return await _dbContext.Ratings
            .Include(r => r.Rater)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
