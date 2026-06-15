using System.Net.Http.Json;
using System.Text.Json;
using TripService.Application.Exceptions;
using TripService.Application;

namespace TripService.Infrastructure;

public class ValhallaRoutingEngine : IRoutingEngine
{
    private readonly HttpClient _http;

    // Valhalla error_code 171 = "No suitable edges near location"
    // This means the routing tiles for the area haven't been built yet.
    private const int NoSuitableEdgesCode = 171;

    private const string TilesNotReadyMessage =
        "Valhalla routing tiles are not ready for the requested area. " +
        "If the container just started, wait for tile building to finish — " +
        "watch 'docker logs -f valhalla' and wait for 'Starting valhalla service!'. " +
        "Building Poland tiles takes ~40 minutes on first run.";

    public ValhallaRoutingEngine(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("valhalla");
    }

    public async Task<RouteResult> GetRouteAsync(LatLngDTO source, LatLngDTO target, CancellationToken ct = default)
    {
        var body = new
        {
            locations = new[]
            {
                new { lon = source.Lng, lat = source.Lat, type = "break", radius = 100 },
                new { lon = target.Lng, lat = target.Lat, type = "break", radius = 100 }
            },
            costing = "auto"
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("/route", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForValhalla400Async(response, ct);
                throw new RoutingEngineUnavailableException(
                    $"Valhalla returned HTTP {(int)response.StatusCode}.");
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var trip      = doc.RootElement.GetProperty("trip");
            var summary   = trip.GetProperty("summary");
            var distanceM = (int)(summary.GetProperty("length").GetDouble() * 1000);
            var durationS = (int)summary.GetProperty("time").GetDouble();
            var encoded   = trip.GetProperty("legs")[0].GetProperty("shape").GetString()!;

            var wkt = DecodePolylineToWkt(encoded);
            return new RouteResult(distanceM, durationS, wkt);
        }
        catch (RoutingEngineUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new RoutingEngineUnavailableException("Valhalla routing engine is unavailable or timed out.");
        }
    }

    public async Task<RouteResult> GetRouteAsync(IReadOnlyList<LatLngDTO> waypoints, CancellationToken ct = default)
    {
        if (waypoints.Count < 2)
            throw new ArgumentException("A route needs at least two waypoints.", nameof(waypoints));

        var body = new
        {
            locations = waypoints
                .Select(w => new { lon = w.Lng, lat = w.Lat, type = "break", radius = 100 })
                .ToArray(),
            costing = "auto"
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("/route", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForValhalla400Async(response, ct);
                throw new RoutingEngineUnavailableException(
                    $"Valhalla returned HTTP {(int)response.StatusCode}.");
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var trip      = doc.RootElement.GetProperty("trip");
            var summary   = trip.GetProperty("summary");
            var distanceM = (int)(summary.GetProperty("length").GetDouble() * 1000);
            var durationS = (int)summary.GetProperty("time").GetDouble();

            // A multi-break route returns one leg per segment; concatenate every leg's shape,
            // dropping the first point of each subsequent leg (it duplicates the previous leg's last).
            var points = new List<(double lat, double lng)>();
            foreach (var leg in trip.GetProperty("legs").EnumerateArray())
            {
                var legPoints = DecodePolyline(leg.GetProperty("shape").GetString()!);
                if (points.Count > 0 && legPoints.Count > 0)
                    legPoints.RemoveAt(0);
                points.AddRange(legPoints);
            }

            return new RouteResult(distanceM, durationS, PointsToWkt(points));
        }
        catch (RoutingEngineUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new RoutingEngineUnavailableException("Valhalla routing engine is unavailable or timed out.");
        }
    }

    public async Task<int?[][]> GetMatrixAsync(LatLngDTO[] sources, LatLngDTO[] targets, CancellationToken ct = default)
    {
        var body = new
        {
            sources = sources.Select(s => new { lon = s.Lng, lat = s.Lat, radius = 1000 }).ToArray(),
            targets = targets.Select(t => new { lon = t.Lng, lat = t.Lat, radius = 1000 }).ToArray(),
            costing = "auto"
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("/sources_to_targets", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                // For tile-not-ready errors on the route endpoint we fail hard.
                // For matrix calls during search, 400 most likely means the passenger's
                // coordinates can't be snapped to a road — return all nulls so Phase 2
                // filters those candidates out rather than crashing the whole search job.
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var allNull = new int?[sources.Length][];
                    for (int i = 0; i < sources.Length; i++)
                        allNull[i] = new int?[targets.Length];
                    return allNull;
                }

                throw new RoutingEngineUnavailableException(
                    $"Valhalla matrix returned HTTP {(int)response.StatusCode}.");
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var rows   = doc.RootElement.GetProperty("sources_to_targets");
            var result = new int?[sources.Length][];

            for (int i = 0; i < sources.Length; i++)
            {
                result[i] = new int?[targets.Length];
                var row = rows[i];
                for (int j = 0; j < targets.Length; j++)
                {
                    var cell = row[j];
                    if (cell.TryGetProperty("distance", out var dist) && dist.ValueKind != JsonValueKind.Null)
                        result[i][j] = (int)(dist.GetDouble() * 1000); // km → m
                }
            }

            return result;
        }
        catch (RoutingEngineUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new RoutingEngineUnavailableException("Valhalla routing engine is unavailable or timed out.");
        }
    }

    // Reads the Valhalla error body and throws a human-readable exception.
    private static async Task ThrowForValhalla400Async(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.BadRequest) return;

        try
        {
            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            if (doc.RootElement.TryGetProperty("error_code", out var code) &&
                code.GetInt32() == NoSuitableEdgesCode)
            {
                throw new RoutingEngineUnavailableException(TilesNotReadyMessage);
            }
        }
        catch (RoutingEngineUnavailableException)
        {
            throw;
        }
        catch
        {
            // Body couldn't be parsed — fall through to generic error
        }
    }

    private static string DecodePolylineToWkt(string encoded, int precision = 6) =>
        PointsToWkt(DecodePolyline(encoded, precision));

    // Decodes a Valhalla-encoded polyline (precision 6) into (lat, lng) points.
    private static List<(double lat, double lng)> DecodePolyline(string encoded, int precision = 6)
    {
        var factor = Math.Pow(10, precision);
        var points = new List<(double lat, double lng)>();
        int index = 0, lat = 0, lng = 0;

        while (index < encoded.Length)
        {
            int b, shift = 0, v = 0;
            do { b = encoded[index++] - 63; v |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
            lat += (v & 1) != 0 ? ~(v >> 1) : v >> 1;

            shift = 0; v = 0;
            do { b = encoded[index++] - 63; v |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
            lng += (v & 1) != 0 ? ~(v >> 1) : v >> 1;

            points.Add((lat / factor, lng / factor));
        }

        return points;
    }

    // Builds a WKT LINESTRING (lng-first, as PostGIS expects) from decoded points.
    private static string PointsToWkt(List<(double lat, double lng)> points)
    {
        if (points.Count < 2)
            throw new RoutingEngineUnavailableException("Valhalla returned an invalid route shape.");

        var coords = string.Join(", ", points.Select(p =>
            $"{p.lng.ToString(System.Globalization.CultureInfo.InvariantCulture)} {p.lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        return $"LINESTRING({coords})";
    }
}

