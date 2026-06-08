using TripService.Application;

namespace TripService.Infrastructure;

// Used in Development mode so the app runs without Valhalla tiles.
// Distances are straight-line Haversine * 1.3 road factor; route polyline is a 2-point straight line.
public class MockRoutingEngine : IRoutingEngine
{
    public Task<RouteResult> GetRouteAsync(LatLngDTO source, LatLngDTO target, CancellationToken ct = default)
    {
        var distanceM = (int)(HaversineMeters(source, target) * 1.3);
        var durationS = (int)(distanceM / (80_000.0 / 3600)); // 80 km/h average
        var wkt = $"LINESTRING({source.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture)} {source.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {target.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture)} {target.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        return Task.FromResult(new RouteResult(distanceM, durationS, wkt));
    }

    public Task<int?[][]> GetMatrixAsync(LatLngDTO[] sources, LatLngDTO[] targets, CancellationToken ct = default)
    {
        var result = new int?[sources.Length][];
        for (int i = 0; i < sources.Length; i++)
        {
            result[i] = new int?[targets.Length];
            for (int j = 0; j < targets.Length; j++)
                result[i][j] = (int)(HaversineMeters(sources[i], targets[j]) * 1.3);
        }
        return Task.FromResult(result);
    }

    private static double HaversineMeters(LatLngDTO a, LatLngDTO b)
    {
        const double R = 6_371_000;
        var dLat = ToRad(b.Lat - a.Lat);
        var dLng = ToRad(b.Lng - a.Lng);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(a.Lat)) * Math.Cos(ToRad(b.Lat))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * R * Math.Asin(Math.Sqrt(h));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
