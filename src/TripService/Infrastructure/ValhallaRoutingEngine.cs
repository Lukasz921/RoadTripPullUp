using System.Net.Http.Json;
using System.Text.Json;
using Application.Exceptions;
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

    public async Task<int?[][]> GetMatrixAsync(LatLngDTO[] sources, LatLngDTO[] targets, CancellationToken ct = default)
    {
        var body = new
        {
            sources = sources.Select(s => new { lon = s.Lng, lat = s.Lat, radius = 100 }).ToArray(),
            targets = targets.Select(t => new { lon = t.Lng, lat = t.Lat, radius = 100 }).ToArray(),
            costing = "auto"
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("/sources_to_targets", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForValhalla400Async(response, ct);
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

    private static string DecodePolylineToWkt(string encoded, int precision = 6)
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

        if (points.Count < 2)
            throw new RoutingEngineUnavailableException("Valhalla returned an invalid route shape.");

        var coords = string.Join(", ", points.Select(p => $"{p.lng} {p.lat}"));
        return $"LINESTRING({coords})";
    }
}
