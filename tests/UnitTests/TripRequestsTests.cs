using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MessageService.Core.Exceptions;
using Moq;
using TripService.Application;
using TripService.Application.Exceptions;
using TripService.Infrastructure;
using Xunit;

namespace UnitTests;

// ---------------------------------------------------------------------------
// Shared helpers for building coordinates and a deterministic distance matrix.
// All test points sit on lat = 0 and vary by lng, so the "road distance" the
// fake routing engine returns is simply |lng difference| — that makes the
// cheapest-insertion ordering easy to reason about and assert exactly.
// ---------------------------------------------------------------------------
internal static class Geo
{
    // A point on the lat=0 line at position `lng`.
    public static LatLngDTO At(double lng) => new() { Lat = 0, Lng = lng };

    public static LatLngDTO LatLng(double lat, double lng) => new() { Lat = lat, Lng = lng };

    // Manhattan distance * 1000, integer meters — symmetric, never null.
    public static int?[][] ManhattanMatrix(LatLngDTO[] sources, LatLngDTO[] targets)
    {
        var m = new int?[sources.Length][];
        for (int i = 0; i < sources.Length; i++)
        {
            m[i] = new int?[targets.Length];
            for (int j = 0; j < targets.Length; j++)
            {
                var d = Math.Abs(sources[i].Lat - targets[j].Lat) + Math.Abs(sources[i].Lng - targets[j].Lng);
                m[i][j] = (int)(d * 1000);
            }
        }
        return m;
    }
}

// ===========================================================================
// MockRoutingEngine — the Development routing engine. The new multi-waypoint
// overload must sum per-leg Haversine*1.3 and emit a multi-point LINESTRING.
// ===========================================================================
public class MockRoutingEngineTests
{
    private readonly MockRoutingEngine _engine = new();

    [Fact]
    public async Task GetRoute_TwoWaypoints_MatchesPointToPointOverload()
    {
        var a = Geo.LatLng(52.2297, 21.0122); // Warsaw
        var b = Geo.LatLng(50.0647, 19.9450); // Krakow

        var pointToPoint = await _engine.GetRouteAsync(a, b);
        var asWaypoints  = await _engine.GetRouteAsync(new[] { a, b });

        asWaypoints.DistanceM.Should().Be(pointToPoint.DistanceM);
        asWaypoints.DurationS.Should().Be(pointToPoint.DurationS);
    }

    [Fact]
    public async Task GetRoute_MultipleWaypoints_SumsEachLeg()
    {
        var a = Geo.LatLng(52.0, 21.0);
        var mid = Geo.LatLng(51.0, 20.5);
        var b = Geo.LatLng(50.0, 20.0);

        var direct = await _engine.GetRouteAsync(new[] { a, b });
        var viaMid = await _engine.GetRouteAsync(new[] { a, mid, b });

        // Going through a detour point is never shorter than the direct hop.
        viaMid.DistanceM.Should().BeGreaterThan(direct.DistanceM);
    }

    [Fact]
    public async Task GetRoute_MultipleWaypoints_ProducesPointPerWaypoint()
    {
        var pts = new[] { Geo.At(0), Geo.At(2), Geo.At(4), Geo.At(6) };

        var result = await _engine.GetRouteAsync(pts);

        result.PolylineWkt.Should().StartWith("LINESTRING(");
        // 4 waypoints -> 4 coordinate pairs -> 3 commas.
        result.PolylineWkt.Split(',').Should().HaveCount(4);
    }

    [Fact]
    public async Task GetRoute_UsesInvariantCulture_ForCoordinates()
    {
        // Decimal points must be '.', never ',', regardless of the host culture.
        var result = await _engine.GetRouteAsync(new[] { Geo.LatLng(50.5, 19.25), Geo.LatLng(51.5, 20.75) });

        result.PolylineWkt.Should().Contain("19.25 50.5");
        result.PolylineWkt.Should().Contain("20.75 51.5");
    }

    [Fact]
    public async Task GetRoute_FewerThanTwoWaypoints_Throws()
    {
        var act = async () => await _engine.GetRouteAsync(new[] { Geo.At(1) });
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetMatrix_ReturnsSymmetricNonNullDistances()
    {
        var pts = new[] { Geo.LatLng(52, 21), Geo.LatLng(50, 20) };

        var m = await _engine.GetMatrixAsync(pts, pts);

        m[0][0].Should().Be(0);
        m[0][1].Should().NotBeNull();
        m[0][1].Should().Be(m[1][0]); // Haversine is symmetric
    }
}

// ===========================================================================
// TripsService.CreateTripRequestAsync — validation, dedup reuse, detour math.
// ===========================================================================
public class TripRequestCreateTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IRoutingEngine> _routing = new();
    private readonly Mock<IJobStore> _jobs = new();
    private readonly Mock<IUserChecker> _users = new();
    private readonly TripsService _service;

    private readonly Guid _driver = Guid.NewGuid();
    private readonly Guid _requester = Guid.NewGuid();
    private readonly Guid _trip = Guid.NewGuid();
    private readonly Guid _conversation = Guid.NewGuid();

    public TripRequestCreateTests()
    {
        _service = new TripsService(_repo.Object, _routing.Object, _jobs.Object, _users.Object);
    }

    private TripDTO TripWithRouteDistance(int routeDistanceM) => new()
    {
        Id = _trip.ToString(),
        DriverId = _driver.ToString(),
        Source = Geo.At(0),
        Target = Geo.At(10),
        RouteDistanceM = routeDistanceM,
        // Detour is measured against the immutable base distance.
        BaseRouteDistanceM = routeDistanceM,
        MaxDetourMeters = 100_000,
    };

    [Fact]
    public async Task Create_ComputesDetour_AsNewRouteMinusOriginal_AndPersists()
    {
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(TripWithRouteDistance(1000));
        _repo.Setup(r => r.IsPassengerAsync(_trip, _requester)).ReturnsAsync(false);
        _repo.Setup(r => r.FindPendingRequestAsync(_trip, _requester)).ReturnsAsync((TripRequestDTO?)null);
        _routing.Setup(r => r.GetRouteAsync(It.IsAny<IReadOnlyList<LatLngDTO>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RouteResult(1500, 90, "LINESTRING(0 0, 1 1)"));

        int? capturedDetour = null;
        _repo.Setup(r => r.InsertTripRequestAsync(
                _trip, _requester, _conversation,
                It.IsAny<LatLngDTO>(), It.IsAny<LatLngDTO>(), It.IsAny<int>(), It.IsAny<RouteResult>()))
            .Callback((Guid _, Guid _, Guid _, LatLngDTO _, LatLngDTO _, int detour, RouteResult _) => capturedDetour = detour)
            .ReturnsAsync(new TripRequestDTO { Id = Guid.NewGuid().ToString(), DetourMeters = 500 });

        var result = await _service.CreateTripRequestAsync(
            _trip.ToString(), _requester.ToString(), _conversation, Geo.At(3), Geo.At(7));

        capturedDetour.Should().Be(500); // 1500 - 1000
        result.DetourMeters.Should().Be(500);
        _repo.Verify(r => r.InsertTripRequestAsync(
            _trip, _requester, _conversation, It.IsAny<LatLngDTO>(), It.IsAny<LatLngDTO>(), 500, It.IsAny<RouteResult>()), Times.Once);
    }

    [Fact]
    public async Task Create_ClampsNegativeDetour_ToZero()
    {
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(TripWithRouteDistance(2000));
        _repo.Setup(r => r.IsPassengerAsync(_trip, _requester)).ReturnsAsync(false);
        _repo.Setup(r => r.FindPendingRequestAsync(_trip, _requester)).ReturnsAsync((TripRequestDTO?)null);
        // New route shorter than the original (degenerate but possible with approximate engines).
        _routing.Setup(r => r.GetRouteAsync(It.IsAny<IReadOnlyList<LatLngDTO>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RouteResult(1800, 90, "LINESTRING(0 0, 1 1)"));

        int? capturedDetour = null;
        _repo.Setup(r => r.InsertTripRequestAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<LatLngDTO>(), It.IsAny<LatLngDTO>(), It.IsAny<int>(), It.IsAny<RouteResult>()))
            .Callback((Guid _, Guid _, Guid _, LatLngDTO _, LatLngDTO _, int detour, RouteResult _) => capturedDetour = detour)
            .ReturnsAsync(new TripRequestDTO());

        await _service.CreateTripRequestAsync(_trip.ToString(), _requester.ToString(), _conversation, Geo.At(3), Geo.At(7));

        capturedDetour.Should().Be(0);
    }

    [Fact]
    public async Task Create_ReusesExistingPendingRequest_WithoutRoutingOrInserting()
    {
        var existing = new TripRequestDTO
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = Guid.NewGuid().ToString(),
            DetourMeters = 4242,
            Status = "PENDING",
        };
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(TripWithRouteDistance(1000));
        _repo.Setup(r => r.IsPassengerAsync(_trip, _requester)).ReturnsAsync(false);
        _repo.Setup(r => r.FindPendingRequestAsync(_trip, _requester)).ReturnsAsync(existing);

        var result = await _service.CreateTripRequestAsync(
            _trip.ToString(), _requester.ToString(), _conversation, Geo.At(3), Geo.At(7));

        result.Should().BeSameAs(existing);
        _routing.Verify(r => r.GetRouteAsync(It.IsAny<IReadOnlyList<LatLngDTO>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.InsertTripRequestAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<LatLngDTO>(), It.IsAny<LatLngDTO>(), It.IsAny<int>(), It.IsAny<RouteResult>()), Times.Never);
    }

    [Fact]
    public async Task Create_DriverRequestingOwnTrip_Throws()
    {
        var trip = TripWithRouteDistance(1000);
        trip.DriverId = _requester.ToString(); // requester IS the driver
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(trip);

        var act = async () => await _service.CreateTripRequestAsync(
            _trip.ToString(), _requester.ToString(), _conversation, Geo.At(3), Geo.At(7));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Create_RequesterAlreadyPassenger_Throws()
    {
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(TripWithRouteDistance(1000));
        _repo.Setup(r => r.IsPassengerAsync(_trip, _requester)).ReturnsAsync(true);

        var act = async () => await _service.CreateTripRequestAsync(
            _trip.ToString(), _requester.ToString(), _conversation, Geo.At(3), Geo.At(7));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Create_TripNotFound_Throws()
    {
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync((TripDTO?)null);

        var act = async () => await _service.CreateTripRequestAsync(
            _trip.ToString(), _requester.ToString(), _conversation, Geo.At(3), Geo.At(7));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_InvalidTripId_Throws()
    {
        var act = async () => await _service.CreateTripRequestAsync(
            "not-a-guid", _requester.ToString(), _conversation, Geo.At(3), Geo.At(7));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_InvalidRequesterId_Throws()
    {
        var act = async () => await _service.CreateTripRequestAsync(
            _trip.ToString(), "not-a-guid", _conversation, Geo.At(3), Geo.At(7));

        await act.Should().ThrowAsync<ValidationException>();
    }
}

// ===========================================================================
// TripsService.AcceptTripRequestAsync — authorization, state checks, and the
// cheapest-insertion route recompute (the heart of the feature).
// ===========================================================================
public class TripRequestAcceptTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IRoutingEngine> _routing = new();
    private readonly Mock<IJobStore> _jobs = new();
    private readonly Mock<IUserChecker> _users = new();
    private readonly TripsService _service;

    private readonly Guid _driver = Guid.NewGuid();
    private readonly Guid _requester = Guid.NewGuid();
    private readonly Guid _trip = Guid.NewGuid();
    private readonly Guid _request = Guid.NewGuid();

    public TripRequestAcceptTests()
    {
        _service = new TripsService(_repo.Object, _routing.Object, _jobs.Object, _users.Object);
        // The matrix always reflects the real coordinates passed in (Manhattan on lng).
        _routing.Setup(r => r.GetMatrixAsync(It.IsAny<LatLngDTO[]>(), It.IsAny<LatLngDTO[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LatLngDTO[] s, LatLngDTO[] t, CancellationToken _) => Geo.ManhattanMatrix(s, t));
    }

    private TripRequestDTO PendingRequest(LatLngDTO pickup, LatLngDTO dropoff) => new()
    {
        Id = _request.ToString(),
        TripId = _trip.ToString(),
        RequesterId = _requester.ToString(),
        Pickup = pickup,
        Dropoff = dropoff,
        Status = "PENDING",
    };

    private TripDTO Trip(LatLngDTO source, LatLngDTO target) => new()
    {
        Id = _trip.ToString(),
        DriverId = _driver.ToString(),
        Source = source,
        Target = target,
        RouteDistanceM = 10_000,
        MaxDetourMeters = 1_000_000,
    };

    // Wires up a happy-path accept and returns the waypoint list the service
    // ends up sending to the routing engine for the final geometry.
    private async Task<List<LatLngDTO>> CaptureWaypointsForAccept(
        LatLngDTO source, LatLngDTO target,
        List<(LatLngDTO Pickup, LatLngDTO Dropoff)> acceptedStops,
        LatLngDTO newPickup, LatLngDTO newDropoff)
    {
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(_driver);
        _repo.Setup(r => r.FindRequestByIdAsync(_request)).ReturnsAsync(PendingRequest(newPickup, newDropoff));
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(Trip(source, target));
        _repo.Setup(r => r.GetAcceptedRequestStopsAsync(_trip)).ReturnsAsync(acceptedStops);
        _repo.Setup(r => r.AcceptTripRequestTransactionalAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RouteResult>()))
            .Returns(Task.CompletedTask);

        List<LatLngDTO> captured = new();
        _routing.Setup(r => r.GetRouteAsync(It.IsAny<IReadOnlyList<LatLngDTO>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<LatLngDTO> wps, CancellationToken _) =>
                {
                    captured = wps.ToList();
                    return new RouteResult(12_345, 678, "LINESTRING(0 0, 1 1)");
                });

        await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());
        return captured;
    }

    [Fact]
    public async Task Accept_FirstPassenger_InsertsPickupThenDropoffBetweenSourceAndTarget()
    {
        var order = await CaptureWaypointsForAccept(
            source: Geo.At(0), target: Geo.At(10),
            acceptedStops: new(),
            newPickup: Geo.At(3), newDropoff: Geo.At(7));

        order.Select(p => p.Lng).Should().Equal(0, 3, 7, 10);
    }

    [Fact]
    public async Task Accept_SecondPassenger_SlotsNewStopsCheaply_WithoutReorderingExisting()
    {
        // Existing accepted passenger p1: pickup@4, dropoff@6  => route is 0,4,6,10.
        // New passenger p2: pickup@2 (near source), dropoff@8 (near target).
        // Cheapest insertion should weave them in: 0,2,4,6,8,10.
        var order = await CaptureWaypointsForAccept(
            source: Geo.At(0), target: Geo.At(10),
            acceptedStops: new() { (Geo.At(4), Geo.At(6)) },
            newPickup: Geo.At(2), newDropoff: Geo.At(8));

        order.Select(p => p.Lng).Should().Equal(0, 2, 4, 6, 8, 10);
    }

    [Fact]
    public async Task Accept_EnforcesPickupBeforeDropoff_EvenWhenGeographicallySuboptimal()
    {
        // pickup@8 (near target), dropoff@2 (near source). Greedy-by-distance alone
        // would place dropoff right after source, but precedence forbids it:
        // pickup is placed first (0,8,10), then dropoff can only go AFTER it -> 0,8,2,10.
        var order = await CaptureWaypointsForAccept(
            source: Geo.At(0), target: Geo.At(10),
            acceptedStops: new(),
            newPickup: Geo.At(8), newDropoff: Geo.At(2));

        order.Select(p => p.Lng).Should().Equal(0, 8, 2, 10);
        var pickupIdx = order.FindIndex(p => p.Lng == 8);
        var dropoffIdx = order.FindIndex(p => p.Lng == 2);
        dropoffIdx.Should().BeGreaterThan(pickupIdx);
    }

    [Fact]
    public async Task Accept_AlwaysKeepsSourceFirstAndTargetLast()
    {
        var order = await CaptureWaypointsForAccept(
            source: Geo.At(0), target: Geo.At(10),
            acceptedStops: new() { (Geo.At(1), Geo.At(9)) },
            newPickup: Geo.At(5), newDropoff: Geo.At(6));

        order.First().Lng.Should().Be(0);
        order.Last().Lng.Should().Be(10);
    }

    [Fact]
    public async Task Accept_PersistsTheRecomputedRoute_AndReturnsRequesterId()
    {
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(_driver);
        _repo.Setup(r => r.FindRequestByIdAsync(_request)).ReturnsAsync(PendingRequest(Geo.At(3), Geo.At(7)));
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(Trip(Geo.At(0), Geo.At(10)));
        _repo.Setup(r => r.GetAcceptedRequestStopsAsync(_trip)).ReturnsAsync(new List<(LatLngDTO, LatLngDTO)>());

        var recomputed = new RouteResult(54_321, 999, "LINESTRING(0 0, 1 1)");
        _routing.Setup(r => r.GetRouteAsync(It.IsAny<IReadOnlyList<LatLngDTO>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recomputed);

        RouteResult? passed = null;
        Guid passedRequester = Guid.Empty;
        _repo.Setup(r => r.AcceptTripRequestTransactionalAsync(
                _trip, _driver, _request, It.IsAny<Guid>(), It.IsAny<RouteResult>()))
            .Callback((Guid _, Guid _, Guid _, Guid requester, RouteResult route) => { passed = route; passedRequester = requester; })
            .Returns(Task.CompletedTask);

        var returned = await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());

        passed.Should().BeSameAs(recomputed);
        passedRequester.Should().Be(_requester);
        returned.Should().Be(_requester.ToString());
    }

    [Fact]
    public async Task Accept_ComputesRouteBeforeOpeningTransaction()
    {
        var calls = new List<string>();
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(_driver);
        _repo.Setup(r => r.FindRequestByIdAsync(_request)).ReturnsAsync(PendingRequest(Geo.At(3), Geo.At(7)));
        _repo.Setup(r => r.FindByIdAsync(_trip)).ReturnsAsync(Trip(Geo.At(0), Geo.At(10)));
        _repo.Setup(r => r.GetAcceptedRequestStopsAsync(_trip)).ReturnsAsync(new List<(LatLngDTO, LatLngDTO)>());
        _routing.Setup(r => r.GetRouteAsync(It.IsAny<IReadOnlyList<LatLngDTO>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => { calls.Add("route"); return new RouteResult(1, 1, "LINESTRING(0 0, 1 1)"); });
        _repo.Setup(r => r.AcceptTripRequestTransactionalAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RouteResult>()))
            .Callback(() => calls.Add("tx")).Returns(Task.CompletedTask);

        await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());

        // The slow routing call must happen before the DB transaction that holds the row lock.
        calls.Should().Equal("route", "tx");
    }

    [Fact]
    public async Task Accept_NonDriver_Throws()
    {
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(Guid.NewGuid()); // a different driver

        var act = async () => await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());

        await act.Should().ThrowAsync<ForbiddenException>();
        _repo.Verify(r => r.AcceptTripRequestTransactionalAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RouteResult>()), Times.Never);
    }

    [Fact]
    public async Task Accept_TripNotFound_Throws()
    {
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync((Guid?)null);

        var act = async () => await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Accept_RequestNotFound_Throws()
    {
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(_driver);
        _repo.Setup(r => r.FindRequestByIdAsync(_request)).ReturnsAsync((TripRequestDTO?)null);

        var act = async () => await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Accept_RequestBelongsToDifferentTrip_Throws()
    {
        var foreign = PendingRequest(Geo.At(3), Geo.At(7));
        foreign.TripId = Guid.NewGuid().ToString(); // not _trip
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(_driver);
        _repo.Setup(r => r.FindRequestByIdAsync(_request)).ReturnsAsync(foreign);

        var act = async () => await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Accept_RequestNotPending_Throws()
    {
        var accepted = PendingRequest(Geo.At(3), Geo.At(7));
        accepted.Status = "ACCEPTED";
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(_driver);
        _repo.Setup(r => r.FindRequestByIdAsync(_request)).ReturnsAsync(accepted);

        var act = async () => await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), _request.ToString());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Accept_InvalidRequestId_Throws()
    {
        _repo.Setup(r => r.GetDriverIdAsync(_trip)).ReturnsAsync(_driver);

        var act = async () => await _service.AcceptTripRequestAsync(_trip.ToString(), _driver.ToString(), "not-a-guid");

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

// ===========================================================================
// TripsService request lookups — thin wrappers that must guard bad GUIDs.
// ===========================================================================
public class TripRequestLookupTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IRoutingEngine> _routing = new();
    private readonly Mock<IJobStore> _jobs = new();
    private readonly Mock<IUserChecker> _users = new();
    private readonly TripsService _service;

    public TripRequestLookupTests()
    {
        _service = new TripsService(_repo.Object, _routing.Object, _jobs.Object, _users.Object);
    }

    [Fact]
    public async Task GetPending_ValidIds_DelegatesToRepository()
    {
        var trip = Guid.NewGuid();
        var requester = Guid.NewGuid();
        var dto = new TripRequestDTO { Id = Guid.NewGuid().ToString(), Status = "PENDING" };
        _repo.Setup(r => r.FindPendingRequestAsync(trip, requester)).ReturnsAsync(dto);

        var result = await _service.GetPendingTripRequestAsync(trip.ToString(), requester.ToString());

        result.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetPending_InvalidIds_ReturnsNull_WithoutHittingRepository()
    {
        var result = await _service.GetPendingTripRequestAsync("nope", Guid.NewGuid().ToString());

        result.Should().BeNull();
        _repo.Verify(r => r.FindPendingRequestAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByConversation_ValidId_DelegatesToRepository()
    {
        var conv = Guid.NewGuid();
        var dto = new TripRequestDTO { Id = Guid.NewGuid().ToString(), ConversationId = conv.ToString() };
        _repo.Setup(r => r.FindRequestByConversationAsync(conv)).ReturnsAsync(dto);

        var result = await _service.GetTripRequestByConversationAsync(conv.ToString());

        result.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetByConversation_InvalidId_ReturnsNull()
    {
        var result = await _service.GetTripRequestByConversationAsync("not-a-guid");

        result.Should().BeNull();
        _repo.Verify(r => r.FindRequestByConversationAsync(It.IsAny<Guid>()), Times.Never);
    }
}

// ===========================================================================
// ValhallaRoutingEngine — driven by a fake HTTP handler so we can assert the
// real request/response handling: km->m conversion, precision-6 polyline
// decoding, and (crucially) concatenation of ALL legs for a multi-waypoint
// route while dropping the duplicated junction point between consecutive legs.
// ===========================================================================
public class ValhallaRoutingEngineTests
{
    // Minimal IHttpClientFactory + handler harness.
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _responder;
        public string? LastPath { get; private set; }
        public string? LastBody { get; private set; }

        public FakeHandler(Func<HttpRequestMessage, string, HttpResponseMessage> responder) => _responder = responder;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastPath = request.RequestUri?.AbsolutePath;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return _responder(request, LastBody ?? "");
        }
    }

    private sealed class FakeFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public FakeFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler) { BaseAddress = new Uri("http://valhalla:8002") };
    }

    private static (ValhallaRoutingEngine engine, FakeHandler handler) Build(
        Func<HttpRequestMessage, string, HttpResponseMessage> responder)
    {
        var handler = new FakeHandler(responder);
        return (new ValhallaRoutingEngine(new FakeFactory(handler)), handler);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // Encodes points to a Valhalla precision-6 polyline (inverse of the engine's decoder).
    private static string EncodePolyline(IEnumerable<(double lat, double lng)> points, int precision = 6)
    {
        var factor = Math.Pow(10, precision);
        var sb = new StringBuilder();
        long lastLat = 0, lastLng = 0;
        foreach (var (lat, lng) in points)
        {
            long la = (long)Math.Round(lat * factor);
            long ln = (long)Math.Round(lng * factor);
            EncodeValue(la - lastLat, sb);
            EncodeValue(ln - lastLng, sb);
            lastLat = la;
            lastLng = ln;
        }
        return sb.ToString();
    }

    private static void EncodeValue(long v, StringBuilder sb)
    {
        long s = v < 0 ? ~(v << 1) : (v << 1);
        while (s >= 0x20)
        {
            sb.Append((char)((0x20 | ((int)s & 0x1f)) + 63));
            s >>= 5;
        }
        sb.Append((char)((int)s + 63));
    }

    private static string RouteBody(double lengthKm, double timeS, params string[] legShapes)
    {
        var legs = string.Join(",", legShapes.Select(s => $"{{\"shape\":\"{s}\"}}"));
        return $"{{\"trip\":{{\"summary\":{{\"length\":{lengthKm.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"time\":{timeS.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},\"legs\":[{legs}]}}}}";
    }

    [Fact]
    public async Task EncodeDecode_RoundTrips_AtPrecision6()
    {
        // Sanity check of the test's own encoder against the engine via a single-leg route.
        var shape = EncodePolyline(new[] { (50.0, 19.0), (50.5, 19.5) });
        var (engine, _) = Build((_, _) => Json(RouteBody(1.0, 60, shape)));

        var result = await engine.GetRouteAsync(new[] { Geo.LatLng(50.0, 19.0), Geo.LatLng(50.5, 19.5) });

        result.PolylineWkt.Should().Contain("19 50");
        result.PolylineWkt.Should().Contain("19.5 50.5");
    }

    [Fact]
    public async Task GetRoute_PointToPoint_ConvertsKmToMeters()
    {
        var shape = EncodePolyline(new[] { (50.0, 19.0), (50.1, 19.1) });
        var (engine, _) = Build((_, _) => Json(RouteBody(lengthKm: 12.5, timeS: 700, shape)));

        var result = await engine.GetRouteAsync(Geo.LatLng(50.0, 19.0), Geo.LatLng(50.1, 19.1));

        result.DistanceM.Should().Be(12_500); // 12.5 km -> 12500 m
        result.DurationS.Should().Be(700);
    }

    [Fact]
    public async Task GetRoute_MultiWaypoint_ConcatenatesAllLegs_DroppingJunctionDuplicate()
    {
        // Two legs that share the middle point (50.1,19.1). The combined polyline
        // must contain that point ONCE, yielding three coordinate pairs total.
        var leg1 = EncodePolyline(new[] { (50.0, 19.0), (50.1, 19.1) });
        var leg2 = EncodePolyline(new[] { (50.1, 19.1), (50.2, 19.2) });

        var (engine, handler) = Build((_, _) => Json(RouteBody(lengthKm: 30.0, timeS: 1800, leg1, leg2)));

        var waypoints = new[] { Geo.LatLng(50.0, 19.0), Geo.LatLng(50.1, 19.1), Geo.LatLng(50.2, 19.2) };
        var result = await engine.GetRouteAsync(waypoints);

        result.DistanceM.Should().Be(30_000);
        result.DurationS.Should().Be(1800);
        result.PolylineWkt.Should().StartWith("LINESTRING(");
        result.PolylineWkt.Split(',').Should().HaveCount(3, "the duplicated junction point is dropped");
        result.PolylineWkt.Should().Contain("19 50");
        result.PolylineWkt.Should().Contain("19.2 50.2");

        // The request must carry all three break locations.
        using var doc = JsonDocument.Parse(handler.LastBody!);
        doc.RootElement.GetProperty("locations").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task GetRoute_MultiWaypoint_FewerThanTwo_Throws()
    {
        var (engine, _) = Build((_, _) => Json(RouteBody(1, 1, EncodePolyline(new[] { (0.0, 0.0), (1.0, 1.0) }))));

        var act = async () => await engine.GetRouteAsync(new[] { Geo.LatLng(1, 1) });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetRoute_NonSuccess_ThrowsRoutingEngineUnavailable()
    {
        var (engine, _) = Build((_, _) => Json("{}", HttpStatusCode.InternalServerError));

        var act = async () => await engine.GetRouteAsync(new[] { Geo.LatLng(50, 19), Geo.LatLng(51, 20) });

        await act.Should().ThrowAsync<RoutingEngineUnavailableException>();
    }

    [Fact]
    public async Task GetMatrix_ParsesDistances_AndConvertsKmToMeters()
    {
        const string body = "{\"sources_to_targets\":[[{\"distance\":1.5},{\"distance\":null}],[{\"distance\":0.0},{\"distance\":2.25}]]}";
        var (engine, _) = Build((_, _) => Json(body));

        var sources = new[] { Geo.LatLng(50, 19), Geo.LatLng(51, 20) };
        var targets = new[] { Geo.LatLng(52, 21), Geo.LatLng(53, 22) };
        var m = await engine.GetMatrixAsync(sources, targets);

        m[0][0].Should().Be(1500);
        m[0][1].Should().BeNull();   // unreachable
        m[1][0].Should().Be(0);
        m[1][1].Should().Be(2250);
    }

    [Fact]
    public async Task GetMatrix_BadRequest_ReturnsAllNulls_RatherThanThrowing()
    {
        var (engine, _) = Build((_, _) => Json("{\"error\":\"bad\"}", HttpStatusCode.BadRequest));

        var sources = new[] { Geo.LatLng(50, 19) };
        var targets = new[] { Geo.LatLng(52, 21), Geo.LatLng(53, 22) };
        var m = await engine.GetMatrixAsync(sources, targets);

        m.Should().HaveCount(1);
        m[0].Should().HaveCount(2);
        m[0][0].Should().BeNull();
        m[0][1].Should().BeNull();
    }
}
