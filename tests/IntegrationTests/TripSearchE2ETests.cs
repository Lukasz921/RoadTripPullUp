using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Application;
using TripService.Infrastructure;

namespace IntegrationTests;

/// <summary>
/// End-to-end search tests that require the full Docker stack:
///   docker compose up -d trip_db valhalla
///
/// Run with:
///   dotnet test --filter "Category=E2E"
/// </summary>
[Trait("Category", "E2E")]
public class TripSearchE2ETests : IAsyncDisposable
{
    private const string TripConnection = "Host=localhost;Port=5433;Database=trip_db;Username=postgres;Password=admin";
    private const string ValhallaUrl    = "http://localhost:8002";

    private readonly List<Guid> _insertedTripIds = new();

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task E2E_PassengerOnRoute_FindsTrip()
    {
        var routing = BuildRealRoutingEngine();
        var service = BuildSearchService(routing);

        // Create trip Warsaw → Kraków via real Valhalla
        var tripService = BuildTripService(routing);
        var trip = await tripService.CreateTripAsync(new CreateTripV1DTO
        {
            Source          = new LatLngDTO { Lat = 52.2297, Lng = 21.0122 },
            Target          = new LatLngDTO { Lat = 50.0647, Lng = 19.9450 },
            DepartureTime   = DateTime.UtcNow.AddDays(30),
            MaxDetourMeters = 50_000,
            PricePerSeat    = 25m,
            AvailableSeats  = 3
        }, Guid.NewGuid().ToString());

        _insertedTripIds.Add(Guid.Parse(trip.Id));

        // Search: Radom → Kraków (Radom is on the Warsaw-Kraków route)
        var result = await service.SearchAsync(new SearchTripsQueryDTO
        {
            SourceLat = 51.4027, SourceLng = 21.1471,  // Radom
            TargetLat = 50.0647, TargetLng = 19.9450,  // Kraków
            DateFrom  = FutureDate(1),
            DateTo    = FutureDate(60),
            MinSeats  = 1,
            Page = 1, PageSize = 20
        });

        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(i => i.Id == trip.Id);
        result.Items.First(i => i.Id == trip.Id).ActualDetourMeters.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task E2E_PassengerOffRoute_FindsNoTrip()
    {
        var routing = BuildRealRoutingEngine();
        var service = BuildSearchService(routing);

        var tripService = BuildTripService(routing);
        var trip = await tripService.CreateTripAsync(new CreateTripV1DTO
        {
            Source          = new LatLngDTO { Lat = 52.2297, Lng = 21.0122 },
            Target          = new LatLngDTO { Lat = 50.0647, Lng = 19.9450 },
            DepartureTime   = DateTime.UtcNow.AddDays(30),
            MaxDetourMeters = 30_000,  // 30 km — Łódź is ~60 km off route
            PricePerSeat    = 25m,
            AvailableSeats  = 3
        }, Guid.NewGuid().ToString());

        _insertedTripIds.Add(Guid.Parse(trip.Id));

        // Search: Łódź → Kraków (Łódź is far west of the Warsaw-Kraków route)
        var result = await service.SearchAsync(new SearchTripsQueryDTO
        {
            SourceLat = 51.7592, SourceLng = 19.4560,  // Łódź
            TargetLat = 50.0647, TargetLng = 19.9450,  // Kraków
            DateFrom  = FutureDate(1),
            DateTo    = FutureDate(60),
            MinSeats  = 1,
            Page = 1, PageSize = 20
        });

        result.Items.Should().NotContain(i => i.Id == trip.Id);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_insertedTripIds.Count == 0) return;

        await using var conn = new NpgsqlConnection(TripConnection);
        await conn.OpenAsync();
        foreach (var id in _insertedTripIds)
        {
            await using var cmd = new NpgsqlCommand("DELETE FROM trip WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IRoutingEngine BuildRealRoutingEngine()
    {
        var http = new HttpClient { BaseAddress = new Uri(ValhallaUrl), Timeout = TimeSpan.FromSeconds(15) };
        return new ValhallaRoutingEngine(new SingleClientFactory(http));
    }

    private static TripsSearchService BuildSearchService(IRoutingEngine routing)
    {
        var config = BuildConfig();
        return new TripsSearchService(config, routing);
    }

    private static TripsV1Service BuildTripService(IRoutingEngine routing)
    {
        var config = BuildConfig();
        // IJobStore not used by CreateTripAsync — pass a no-op stub
        return new TripsV1Service(config, routing, new NoOpJobStore(), new NoOpUserChecker());
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TripConnection"]  = TripConnection,
                ["ConnectionStrings:DefaultConnection"] = TripConnection
            })
            .Build();

    private static string FutureDate(int days) =>
        DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd");

    // Wraps a single HttpClient as IHttpClientFactory
    private sealed class SingleClientFactory : System.Net.Http.IHttpClientFactory
    {
        private readonly HttpClient _client;
        public SingleClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    // No-op stubs for dependencies not needed in these tests
    private sealed class NoOpJobStore : IJobStore
    {
        public Task EnsureConsumerGroupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<string> EnqueueAsync(string userId, SearchTripsQueryDTO query, CancellationToken ct = default) => Task.FromResult(string.Empty);
        public Task<SearchJob?> GetJobAsync(string jobId, string requestingUserId, CancellationToken ct = default) => Task.FromResult<SearchJob?>(null);
        public Task<IReadOnlyList<PendingJob>> DequeueAsync(int count, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PendingJob>>(Array.Empty<PendingJob>());
        public Task SetProcessingAsync(string jobId, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetDoneAsync(string jobId, SyncSearchResultDTO result, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetErrorAsync(string jobId, string error, CancellationToken ct = default) => Task.CompletedTask;
        public Task AcknowledgeAsync(string messageId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoOpUserChecker : IUserChecker
    {
        public Task<bool> UserExistsAsync(string userId, CancellationToken ct = default) => Task.FromResult(true);
    }
}
