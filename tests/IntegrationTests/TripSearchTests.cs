using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using TripService.Application;
using TripService.Infrastructure;

namespace IntegrationTests;

public class TripSearchTests : IAsyncLifetime
{
    // Warsaw → Kraków trip: straight-line polyline, max_detour_m = 50 km
    // On-route passenger: Radom (~34 km from the straight line → passes Phase 1)
    // Off-route passenger: Łódź  (~82 km from the straight line → fails Phase 1)

    private const int MaxDetourM     = 50_000;
    private const int RouteDistanceM = 295_000;

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:16-3.4")
        .WithDatabase("trip_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        await RunSqlFileAsync("sql/001_create_trip.sql");
        await RunSqlFileAsync("sql/002_add_trip_passengers.sql");
    }

    public async Task DisposeAsync() => await _db.StopAsync();

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_PassengerOnRoute_FindsTrip()
    {
        await InsertTripAsync();

        // Mock Phase 2 matrix calls.
        // Detour = leg1 + leg3 + leg2 − route_distance_m
        //        = 110 000 + 180 000 + 10 000 − 295 000 = 5 000 m  → within 50 km ✓
        var routing = BuildRoutingMock(leg1: 110_000, leg2: 10_000, leg3: 180_000);
        var service = BuildSearchService(routing);

        var result = await service.SearchAsync(new SearchTripsQueryDTO
        {
            SourceLat = 51.4027, SourceLng = 21.1471,  // Radom
            TargetLat = 50.0647, TargetLng = 19.9450,  // Kraków
            DateFrom  = FutureDate(1),
            DateTo    = FutureDate(60),
            MinSeats  = 1,
            Page = 1, PageSize = 20
        });

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].ActualDetourMeters.Should().Be(5_000);
    }

    [Fact]
    public async Task Search_PassengerOffRoute_FindsNoTrip()
    {
        await InsertTripAsync();

        // Routing mock should never be called — Łódź is filtered out in Phase 1.
        var routing = new Mock<IRoutingEngine>();
        var service = BuildSearchService(routing.Object);

        var result = await service.SearchAsync(new SearchTripsQueryDTO
        {
            SourceLat = 51.7592, SourceLng = 19.4560,  // Łódź (~82 km from line)
            TargetLat = 50.0647, TargetLng = 19.9450,  // Kraków
            DateFrom  = FutureDate(1),
            DateTo    = FutureDate(60),
            MinSeats  = 1,
            Page = 1, PageSize = 20
        });

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        routing.Verify(
            r => r.GetMatrixAsync(It.IsAny<LatLngDTO[]>(), It.IsAny<LatLngDTO[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task InsertTripAsync()
    {
        const string sql = """
            INSERT INTO trip (
                driver_user_id, source_geog, target_geog, route_polyline,
                route_distance_m, route_duration_s, max_detour_m,
                departure_time, price_per_seat, available_seats
            )
            VALUES (
                gen_random_uuid(),
                ST_SetSRID(ST_MakePoint(21.0122, 52.2297), 4326)::geography,
                ST_SetSRID(ST_MakePoint(19.9450, 50.0647), 4326)::geography,
                ST_GeomFromText('LINESTRING(21.0122 52.2297, 19.9450 50.0647)', 4326)::geography,
                @routeDistanceM, 10620, @maxDetourM,
                @departureTime, 25.00, 3
            )
            """;

        await using var conn = new NpgsqlConnection(_db.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("routeDistanceM", RouteDistanceM);
        cmd.Parameters.AddWithValue("maxDetourM",     MaxDetourM);
        cmd.Parameters.AddWithValue("departureTime",  DateTime.SpecifyKind(DateTime.UtcNow.AddDays(30), DateTimeKind.Utc));
        await cmd.ExecuteNonQueryAsync();
    }

    private IRoutingEngine BuildRoutingMock(int leg1, int leg2, int leg3)
    {
        var mock = new Mock<IRoutingEngine>();

        mock.Setup(r => r.GetMatrixAsync(
                It.IsAny<LatLngDTO[]>(),
                It.IsAny<LatLngDTO[]>(),
                It.IsAny<CancellationToken>()))
            .Returns((LatLngDTO[] sources, LatLngDTO[] targets, CancellationToken _) =>
            {
                // leg1: tripSources → passengerSource   (source is the trip driver's start: Warsaw ~52.2)
                // leg2: passengerTarget → tripTargets   (source is passenger target: Kraków ~50.06)
                // leg3: passengerSource → passengerTarget (source is passenger source: Radom ~51.4)
                var srcLat = sources[0].Lat;
                int?[][] result = Math.Abs(srcLat - 52.2297) < 0.1
                    ? new[] { new int?[] { leg1 } }   // leg1
                    : Math.Abs(srcLat - 50.0647) < 0.1
                        ? new[] { new int?[] { leg2 } }   // leg2
                        : new[] { new int?[] { leg3 } };  // leg3
                return Task.FromResult(result);
            });

        return mock.Object;
    }

    private TripsSearchService BuildSearchService(IRoutingEngine routing)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TripConnection"] = _db.GetConnectionString()
            })
            .Build();

        return new TripsSearchService(config, routing);
    }

    private async Task RunSqlFileAsync(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, relativePath);
        var sql  = await File.ReadAllTextAsync(path);
        await using var conn = new NpgsqlConnection(_db.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string FutureDate(int daysFromNow) =>
        DateTime.UtcNow.AddDays(daysFromNow).ToString("yyyy-MM-dd");
}
