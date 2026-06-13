using Users.Core;

namespace Users.Application.Interfaces;

public interface IRatingRepository
{
    Task Add(Rating rating);
    Task<List<Rating>> GetByUserId(Guid userId);
}
