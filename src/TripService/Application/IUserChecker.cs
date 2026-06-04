namespace TripService.Application;

public interface IUserChecker
{
    Task<bool> UserExistsAsync(string userId, CancellationToken ct = default);
}
