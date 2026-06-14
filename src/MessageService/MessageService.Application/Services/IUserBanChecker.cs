namespace MessageService.Application.Services;

public interface IUserBanChecker
{
    Task<bool> IsUserBannedAsync(Guid userId, CancellationToken ct = default);
}
