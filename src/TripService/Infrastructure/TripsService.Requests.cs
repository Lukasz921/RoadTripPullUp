using MessageService.Core.Exceptions;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsService
{
    public async Task<TripRequestDTO> CreateTripRequestAsync(
        string tripId, string requesterId, Guid conversationId, LatLngDTO pickup, LatLngDTO dropoff)
    {
        if (!Guid.TryParse(tripId, out var tripGuid))
            throw new NotFoundException($"Trip '{tripId}' not found.");
        if (!Guid.TryParse(requesterId, out var requesterGuid))
            throw new ValidationException("requesterId is not a valid UUID.");

        var trip = await _repository.FindByIdAsync(tripGuid)
            ?? throw new NotFoundException($"Trip '{tripId}' not found.");

        if (Guid.Parse(trip.DriverId) == requesterGuid)
            throw new ValidationException("You cannot request a ride on your own trip.");
        if (await _repository.IsPassengerAsync(tripGuid, requesterGuid))
            throw new ValidationException("You are already a passenger on this trip.");

        // Reuse an existing open request (the partial unique index is the DB-level backstop).
        var existing = await _repository.FindPendingRequestAsync(tripGuid, requesterGuid);
        if (existing is not null)
            return existing;

        // Preview route source -> pickup -> dropoff -> target; detour vs the driver's ORIGINAL
        // route (base distance), so it stays consistent no matter how many passengers already joined.
        var preview = await _routing.GetRouteAsync(new[] { trip.Source, pickup, dropoff, trip.Target });
        var detour  = Math.Max(0, preview.DistanceM - trip.BaseRouteDistanceM);

        return await _repository.InsertTripRequestAsync(
            tripGuid, requesterGuid, conversationId, pickup, dropoff, detour, preview);
    }

    public async Task<TripRequestDTO?> GetPendingTripRequestAsync(string tripId, string requesterId)
    {
        if (!Guid.TryParse(tripId, out var tripGuid) || !Guid.TryParse(requesterId, out var requesterGuid))
            return null;
        return await _repository.FindPendingRequestAsync(tripGuid, requesterGuid);
    }

    public async Task<TripRequestDTO?> GetTripRequestByConversationAsync(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var convGuid))
            return null;
        return await _repository.FindRequestByConversationAsync(convGuid);
    }

    public async Task<string> AcceptTripRequestAsync(string tripId, string driverId, string requestId)
    {
        if (!Guid.TryParse(tripId, out var tripGuid))
            throw new NotFoundException($"Trip '{tripId}' not found.");
        if (!Guid.TryParse(driverId, out var driverGuid))
            throw new ForbiddenException("Invalid driver identity.");
        if (!Guid.TryParse(requestId, out var requestGuid))
            throw new NotFoundException($"Request '{requestId}' not found.");

        var ownerId = await _repository.GetDriverIdAsync(tripGuid)
            ?? throw new NotFoundException($"Trip '{tripId}' not found.");
        if (ownerId != driverGuid)
            throw new ForbiddenException("Only the trip driver can accept requests.");

        var request = await _repository.FindRequestByIdAsync(requestGuid);
        if (request is null || request.TripId != tripId)
            throw new NotFoundException($"Request '{requestId}' not found.");
        if (request.Status != "PENDING")
            throw new ValidationException("This request is no longer pending.");

        var trip = await _repository.FindByIdAsync(tripGuid)
            ?? throw new NotFoundException($"Trip '{tripId}' not found.");
        var acceptedStops = await _repository.GetAcceptedRequestStopsAsync(tripGuid);

        // Build the new stop order (cheapest insertion, road-distance scored), compute the route
        // BEFORE the transaction so we don't hold the trip row lock across the routing-engine call.
        var waypoints = await BuildWaypointsAsync(trip.Source, trip.Target, acceptedStops, request.Pickup, request.Dropoff);
        var newRoute  = await _routing.GetRouteAsync(waypoints);

        await _repository.AcceptTripRequestTransactionalAsync(
            tripGuid, driverGuid, requestGuid, Guid.Parse(request.RequesterId), newRoute);

        return request.RequesterId;
    }

    // Orders all stops as source -> [accepted stops...] -> new stop -> target using cheapest insertion:
    // each pickup/dropoff is slotted into the gap that adds the least road distance (one matrix call),
    // keeping "pickup before its own dropoff" and never reordering already-accepted passengers.
    private async Task<List<LatLngDTO>> BuildWaypointsAsync(
        LatLngDTO source, LatLngDTO target,
        List<(LatLngDTO Pickup, LatLngDTO Dropoff)> acceptedStops,
        LatLngDTO newPickup, LatLngDTO newDropoff)
    {
        // points[0]=source, points[1]=target, then each stop's pickup/dropoff pair.
        var points = new List<LatLngDTO> { source, target };
        var stops  = new List<(int Pickup, int Dropoff)>();
        foreach (var s in acceptedStops)
        {
            points.Add(s.Pickup); points.Add(s.Dropoff);
            stops.Add((points.Count - 2, points.Count - 1));
        }
        points.Add(newPickup); points.Add(newDropoff);
        stops.Add((points.Count - 2, points.Count - 1));

        var matrix = await _routing.GetMatrixAsync(points.ToArray(), points.ToArray());
        double Dist(int a, int b) => matrix[a][b] ?? double.PositiveInfinity;

        var route = new List<int> { 0, 1 }; // source ... target; source stays first, target last
        foreach (var (pickup, dropoff) in stops)
        {
            var pickupPos = InsertCheapest(route, pickup, afterPos: 0, Dist);
            InsertCheapest(route, dropoff, afterPos: pickupPos, Dist);
        }

        return route.Select(i => points[i]).ToList();
    }

    // Inserts `point` into the cheapest gap at index > afterPos; returns its new position.
    private static int InsertCheapest(List<int> route, int point, int afterPos, Func<int, int, double> dist)
    {
        var best = double.PositiveInfinity;
        var bestPos = -1;
        for (int i = afterPos; i < route.Count - 1; i++)
        {
            var delta = dist(route[i], point) + dist(point, route[i + 1]) - dist(route[i], route[i + 1]);
            if (delta < best) { best = delta; bestPos = i + 1; }
        }
        if (bestPos == -1) bestPos = route.Count - 1; // all gaps unreachable: drop just before target
        route.Insert(bestPos, point);
        return bestPos;
    }
}
