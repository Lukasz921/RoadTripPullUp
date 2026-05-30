namespace TripService.Application;

public record RouteResult(int DistanceM, int DurationS, string PolylineWkt);

public interface IRoutingEngine
{
    Task<RouteResult> GetRouteAsync(LatLngDTO source, LatLngDTO target, CancellationToken ct = default);
}
