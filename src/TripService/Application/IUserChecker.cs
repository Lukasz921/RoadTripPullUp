namespace TripService.Application;

public interface IUserChecker
{
    Task<bool> UserExistsAsync(string userId, CancellationToken ct = default);
    Task<bool> IsUserBannedAsync(string userId, CancellationToken ct = default);
}
