namespace TripService.Application;

public record RouteResult(int DistanceM, int DurationS, string PolylineWkt);

public interface IRoutingEngine
{
    Task<RouteResult> GetRouteAsync(LatLngDTO source, LatLngDTO target, CancellationToken ct = default);

    // Route through an ordered list of waypoints (>= 2): source, [intermediate stops...], target.
    // Distance/duration are the totals across all legs; the polyline spans the whole route.
    Task<RouteResult> GetRouteAsync(IReadOnlyList<LatLngDTO> waypoints, CancellationToken ct = default);

    // Returns distance matrix in meters: result[sourceIdx][targetIdx], null = unreachable.
    Task<int?[][]> GetMatrixAsync(LatLngDTO[] sources, LatLngDTO[] targets, CancellationToken ct = default);
}
