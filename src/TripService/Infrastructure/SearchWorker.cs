using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TripService.Application;

namespace TripService.Infrastructure;

public class SearchWorker : BackgroundService
{
    private readonly IJobStore _store;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SearchWorker> _logger;

    public SearchWorker(IJobStore store, IServiceScopeFactory scopes, ILogger<SearchWorker> logger)
    {
        _store  = store;
        _scopes = scopes;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        await _store.EnsureConsumerGroupAsync(ct);
        await base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<PendingJob> jobs;
            try
            {
                jobs = await _store.DequeueAsync(count: 5, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to dequeue search jobs");
                await Task.Delay(2_000, stoppingToken);
                continue;
            }

            if (jobs.Count == 0)
            {
                await Task.Delay(200, stoppingToken);
                continue;
            }

            // Process each job concurrently within the batch
            await Task.WhenAll(jobs.Select(job => ProcessJobAsync(job, stoppingToken)));
        }
    }

    private async Task ProcessJobAsync(PendingJob job, CancellationToken ct)
    {
        _logger.LogInformation("Processing search job {JobId}", job.JobId);
        await _store.SetProcessingAsync(job.JobId, ct);

        try
        {
            using var scope  = _scopes.CreateScope();
            var search = scope.ServiceProvider.GetRequiredService<ITripsSearchService>();

            var result = await search.SearchAsync(job.Query, ct);
            await _store.SetDoneAsync(job.JobId, result, ct);
            _logger.LogInformation("Search job {JobId} done — {Count} results", job.JobId, result.TotalCount);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Search job {JobId} failed", job.JobId);
            await _store.SetErrorAsync(job.JobId, ex.Message, ct);
        }
        finally
        {
            await _store.AcknowledgeAsync(job.MessageId, ct);
        }
    }
}
