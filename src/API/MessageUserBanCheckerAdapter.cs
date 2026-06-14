using MessageService.Application.Services;
using Users.Application.Interfaces;

namespace API;

public class MessageUserBanCheckerAdapter : IUserBanChecker
{
    private readonly IUserService _users;

    public MessageUserBanCheckerAdapter(IUserService users)
    {
        _users = users;
    }

    public async Task<bool> IsUserBannedAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _users.GetById(userId);
            return user.IsBanned && (user.BannedUntil == null || user.BannedUntil > DateTime.UtcNow);
        }
        catch
        {
            return false;
        }
    }
}
