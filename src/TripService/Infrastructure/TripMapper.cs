using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

internal static class TripMapper
{
    internal static async Task<PagedTripsDTO> ReadPagedAsync(NpgsqlCommand cmd, int page, int pageSize)
    {
        var items = new List<TripDTO>();
        var totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (items.Count == 0)
                totalCount = reader.GetInt32(reader.GetOrdinal("total_count"));
            items.Add(MapRowWithPassengers(reader));
        }

        return new PagedTripsDTO
        {
            Items      = items,
            Page       = page,
            PageSize   = pageSize,
            TotalCount = totalCount
        };
    }

    internal static TripDTO MapRowWithPassengers(NpgsqlDataReader r) =>
        MapRow(r, r.GetFieldValue<Guid[]>(r.GetOrdinal("passenger_ids"))
                  .Select(g => g.ToString())
                  .ToList());

    internal static TripDTO MapRowDetail(NpgsqlDataReader r)
    {
        var dto = MapRowWithPassengers(r);

        var geoJsonOrd = r.GetOrdinal("route_geojson");
        if (!r.IsDBNull(geoJsonOrd))
        {
            var geoJson = r.GetString(geoJsonOrd);
            using var doc = System.Text.Json.JsonDocument.Parse(geoJson);
            var coords = doc.RootElement.GetProperty("coordinates");
            dto.RoutePolylinePoints = coords.EnumerateArray()
                .Select(c =>
                {
                    var arr = c.EnumerateArray().ToArray();
                    return new LatLngDTO { Lng = arr[0].GetDouble(), Lat = arr[1].GetDouble() };
                })
                .ToList();
        }

        return dto;
    }

    internal static TripDTO MapRow(NpgsqlDataReader r, List<string> passengerIds) => new()
    {
        Id              = r.GetGuid(r.GetOrdinal("id")).ToString(),
        DriverId        = r.GetGuid(r.GetOrdinal("driver_user_id")).ToString(),
        Source          = new LatLngDTO { Lat = r.GetDouble(r.GetOrdinal("source_lat")), Lng = r.GetDouble(r.GetOrdinal("source_lng")) },
        Target          = new LatLngDTO { Lat = r.GetDouble(r.GetOrdinal("target_lat")), Lng = r.GetDouble(r.GetOrdinal("target_lng")) },
        RouteDistanceM  = r.GetInt32(r.GetOrdinal("route_distance_m")),
        RouteDurationS  = r.GetInt32(r.GetOrdinal("route_duration_s")),
        MaxDetourMeters = r.GetInt32(r.GetOrdinal("max_detour_m")),
        DepartureTime   = r.GetDateTime(r.GetOrdinal("departure_time")),
        PricePerSeat    = r.GetDecimal(r.GetOrdinal("price_per_seat")),
        AvailableSeats  = r.GetInt16(r.GetOrdinal("available_seats")),
        Status          = r.GetString(r.GetOrdinal("status")),
        CreatedAt       = r.GetDateTime(r.GetOrdinal("created_at")),
        PassengerIds    = passengerIds
    };
}
