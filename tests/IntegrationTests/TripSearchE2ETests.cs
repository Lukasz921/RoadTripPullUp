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
    public async Task E2E_Phase1PassesPhase2Rejects_BackwardsPassenger()
    {
        // Driver: Warsaw → Kraków (south), max_detour_m = 50 km
        // Passenger: Radom (on the route) → Warsaw (going BACKWARDS north)
        //
        // Phase 1 (ST_DWithin): passes — Radom is within 50 km of the polyline,
        //   and Warsaw (pax target) is the driver's own start point, so distance = 0.
        //
        // Phase 2 (Valhalla matrix detour):
        //   detour = leg1(Warsaw→Radom) + leg3(Radom→Warsaw) + leg2(Warsaw→Kraków) − route(Warsaw→Kraków)
        //          ≈       103 km       +        103 km       +        295 km        −        295 km
        //          = 206 km >> 50 km  →  REJECTED by Phase 2

        var routing    = BuildRealRoutingEngine();
        var tripSvc    = BuildTripService(routing);
        var searchSvc  = BuildSearchService(routing);

        var trip = await tripSvc.CreateTripAsync(new CreateTripV1DTO
        {
            Source          = new LatLngDTO { Lat = 52.2297, Lng = 21.0122 }, // Warsaw
            Target          = new LatLngDTO { Lat = 50.0647, Lng = 19.9450 }, // Kraków
            DepartureTime   = DateTime.UtcNow.AddDays(30),
            MaxDetourMeters = 50_000,
            PricePerSeat    = 25m,
            AvailableSeats  = 3
        }, Guid.NewGuid().ToString());

        _insertedTripIds.Add(Guid.Parse(trip.Id));

        // Verify Phase 1 actually passes — the trip IS a candidate
        var phase1Candidates = await CountPhase1CandidatesAsync(
            srcLat: 51.4027, srcLng: 21.1471, // Radom
            tgtLat: 52.2297, tgtLng: 21.0122, // Warsaw (backwards)
            dateFrom: FutureDate(1), dateTo: FutureDate(60));

        phase1Candidates.Should().BeGreaterThan(0,
            "Radom and Warsaw are both within 50 km of the Warsaw→Kraków polyline, so Phase 1 must accept the candidate");

        // Full search — Phase 2 must reject it
        var result = await searchSvc.SearchAsync(new SearchTripsQueryDTO
        {
            SourceLat = 51.4027, SourceLng = 21.1471, // Radom
            TargetLat = 52.2297, TargetLng = 21.0122, // Warsaw — backwards!
            DateFrom  = FutureDate(1),
            DateTo    = FutureDate(60),
            MinSeats  = 1,
            Page = 1, PageSize = 20
        });

        result.Items.Should().NotContain(i => i.Id == trip.Id,
            "Phase 2 must reject: backwards detour ≈ 206 km, far above 50 km max");
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

    // Runs Phase 1 SQL directly against trip_db and returns candidate count.
    // Use this to prove a candidate exists before Phase 2 filters it.
    private async Task<int> CountPhase1CandidatesAsync(
        double srcLat, double srcLng,
        double tgtLat, double tgtLng,
        string dateFrom, string dateTo)
    {
        const string sql = """
            SELECT COUNT(*) FROM trip t
            WHERE t.status = 'ACTIVE'
              AND t.departure_time >= @dateFrom
              AND t.departure_time <= @dateTo
              AND ST_DWithin(
                    t.route_polyline,
                    ST_SetSRID(ST_MakePoint(@srcLng, @srcLat), 4326)::geography,
                    t.max_detour_m)
              AND ST_DWithin(
                    t.route_polyline,
                    ST_SetSRID(ST_MakePoint(@tgtLng, @tgtLat), 4326)::geography,
                    t.max_detour_m)
            """;

        await using var conn = new NpgsqlConnection(TripConnection);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("srcLat",   srcLat);
        cmd.Parameters.AddWithValue("srcLng",   srcLng);
        cmd.Parameters.AddWithValue("tgtLat",   tgtLat);
        cmd.Parameters.AddWithValue("tgtLng",   tgtLng);
        cmd.Parameters.AddWithValue("dateFrom", DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1), DateTimeKind.Utc));
        cmd.Parameters.AddWithValue("dateTo",   DateTime.SpecifyKind(DateTime.UtcNow.AddDays(60), DateTimeKind.Utc));

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
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
