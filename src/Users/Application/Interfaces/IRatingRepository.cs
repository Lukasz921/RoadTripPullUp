using Users.Core;

namespace Users.Application.Interfaces;

public interface IRatingRepository
{
    Task Add(Rating rating);
    Task<Rating?> GetById(Guid id);
    Task<List<Rating>> GetByUserId(Guid userId);
    Task Delete(Rating rating);
}
