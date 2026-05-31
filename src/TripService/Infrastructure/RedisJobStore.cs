using System.Text.Json;
using StackExchange.Redis;
using TripService.Application;

namespace TripService.Infrastructure;

public class RedisJobStore : IJobStore
{
    private const string StreamKey    = "trips:search:queue";
    private const string GroupName    = "workers";
    private const string ConsumerName = "worker-0";
    private const int    JobTtlSec    = 3_600; // 1 hour

    private readonly IDatabase _db;

    public RedisJobStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task EnsureConsumerGroupAsync(CancellationToken ct = default)
    {
        try
        {
            // createStream: true creates the stream key if it doesn't exist yet
            await _db.StreamCreateConsumerGroupAsync(StreamKey, GroupName, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.StartsWith("BUSYGROUP"))
        {
            // Group already exists — fine
        }
    }

    public async Task<string> EnqueueAsync(string userId, SearchTripsQueryDTO query, CancellationToken ct = default)
    {
        var jobId     = Guid.NewGuid().ToString("N");
        var queryJson = JsonSerializer.Serialize(query);
        var key       = JobKey(jobId);

        await _db.HashSetAsync(key, new HashEntry[]
        {
            new("status",    "pending"),
            new("userId",    userId),
            new("query",     queryJson),
            new("createdAt", DateTime.UtcNow.ToString("O"))
        });
        await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(JobTtlSec));
        await _db.StreamAddAsync(StreamKey, new[] { new NameValueEntry("jobId", jobId) });

        return jobId;
    }

    public async Task<SearchJob?> GetJobAsync(string jobId, CancellationToken ct = default)
    {
        var fields = await _db.HashGetAllAsync(JobKey(jobId));
        if (fields.Length == 0) return null;

        var d = fields.ToDictionary(f => f.Name.ToString(), f => f.Value.ToString());

        var job = new SearchJob
        {
            JobId  = jobId,
            UserId = d.GetValueOrDefault("userId", ""),
            Status = d.GetValueOrDefault("status", "pending"),
            Error  = d.GetValueOrDefault("error")
        };

        if (d.TryGetValue("createdAt", out var ca) && DateTime.TryParse(ca, out var createdAt))
            job.CreatedAt = createdAt;

        if (d.TryGetValue("completedAt", out var coa) && DateTime.TryParse(coa, out var completedAt))
            job.CompletedAt = completedAt;

        if (d.TryGetValue("result", out var resultJson) && !string.IsNullOrEmpty(resultJson))
            job.Result = JsonSerializer.Deserialize<SyncSearchResultDTO>(resultJson);

        return job;
    }

    public async Task<IReadOnlyList<PendingJob>> DequeueAsync(int count, CancellationToken ct = default)
    {
        // ">" means: messages not yet delivered to any consumer in this group
        var messages = await _db.StreamReadGroupAsync(
            StreamKey, GroupName, ConsumerName, ">", count);

        if (messages.Length == 0) return Array.Empty<PendingJob>();

        var result = new List<PendingJob>(messages.Length);
        foreach (var msg in messages)
        {
            var jobId = msg.Values.FirstOrDefault(v => v.Name == "jobId").Value.ToString();
            if (string.IsNullOrEmpty(jobId)) continue;

            var fields = await _db.HashGetAllAsync(JobKey(jobId));
            if (fields.Length == 0) continue;

            var d = fields.ToDictionary(f => f.Name.ToString(), f => f.Value.ToString());
            if (!d.TryGetValue("query",  out var queryJson)) continue;
            if (!d.TryGetValue("userId", out var userId))   continue;

            var query = JsonSerializer.Deserialize<SearchTripsQueryDTO>(queryJson);
            if (query == null) continue;

            result.Add(new PendingJob(msg.Id.ToString(), jobId, userId, query));
        }

        return result;
    }

    public Task SetProcessingAsync(string jobId, CancellationToken ct = default) =>
        _db.HashSetAsync(JobKey(jobId), "status", "processing");

    public async Task SetDoneAsync(string jobId, SyncSearchResultDTO result, CancellationToken ct = default)
    {
        var key = JobKey(jobId);
        await _db.HashSetAsync(key, new HashEntry[]
        {
            new("status",      "done"),
            new("completedAt", DateTime.UtcNow.ToString("O")),
            new("result",      JsonSerializer.Serialize(result))
        });
        await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(JobTtlSec));
    }

    public async Task SetErrorAsync(string jobId, string error, CancellationToken ct = default)
    {
        var key = JobKey(jobId);
        await _db.HashSetAsync(key, new HashEntry[]
        {
            new("status",      "error"),
            new("completedAt", DateTime.UtcNow.ToString("O")),
            new("error",       error)
        });
        await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(JobTtlSec));
    }

    public Task AcknowledgeAsync(string messageId, CancellationToken ct = default) =>
        _db.StreamAcknowledgeAsync(StreamKey, GroupName, messageId);

    private static string JobKey(string jobId) => $"trips:search:job:{jobId}";
}
