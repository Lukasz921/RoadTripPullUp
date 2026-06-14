namespace MessageService.Application.Services;

public class NoOpUserBanChecker : IUserBanChecker
{
    public Task<bool> IsUserBannedAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(false);
}
