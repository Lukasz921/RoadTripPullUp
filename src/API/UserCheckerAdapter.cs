using TripService.Application;
using Users.Application.Interfaces;

namespace API;

public class UserCheckerAdapter : IUserChecker
{
    private readonly IUserService _users;

    public UserCheckerAdapter(IUserService users)
    {
        _users = users;
    }

    public async Task<bool> UserExistsAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var guid)) return false;

        try
        {
            await _users.GetById(guid);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsUserBannedAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var guid)) return false;

        try
        {
            var user = await _users.GetById(guid);
            return user.IsBanned && (user.BannedUntil == null || user.BannedUntil > DateTime.UtcNow);
        }
        catch
        {
            return false;
        }
    }
}
