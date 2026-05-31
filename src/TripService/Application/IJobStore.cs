namespace TripService.Application;

public class SearchJob
{
    public string JobId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending | processing | done | error
    public SyncSearchResultDTO? Result { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public record PendingJob(string MessageId, string JobId, string UserId, SearchTripsQueryDTO Query);

public interface IJobStore
{
    Task EnsureConsumerGroupAsync(CancellationToken ct = default);
    Task<string> EnqueueAsync(string userId, SearchTripsQueryDTO query, CancellationToken ct = default);
    Task<SearchJob?> GetJobAsync(string jobId, CancellationToken ct = default);
    Task<IReadOnlyList<PendingJob>> DequeueAsync(int count, CancellationToken ct = default);
    Task SetProcessingAsync(string jobId, CancellationToken ct = default);
    Task SetDoneAsync(string jobId, SyncSearchResultDTO result, CancellationToken ct = default);
    Task SetErrorAsync(string jobId, string error, CancellationToken ct = default);
    Task AcknowledgeAsync(string messageId, CancellationToken ct = default);
}
