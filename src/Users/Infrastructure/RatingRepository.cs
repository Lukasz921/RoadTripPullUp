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
}
