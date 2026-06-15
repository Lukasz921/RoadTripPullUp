namespace TripService.Application;

public class TripDTO
{
    public string Id { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public LatLngDTO Source { get; set; } = new();
    public LatLngDTO Target { get; set; } = new();
    public DateTime DepartureTime { get; set; }
    public int RouteDistanceM { get; set; }
    public int RouteDurationS { get; set; }
    // Original driver-only route distance; immutable baseline for detour math.
    public int BaseRouteDistanceM { get; set; }
    public int MaxDetourMeters { get; set; }
    public decimal PricePerSeat { get; set; }
    public int AvailableSeats { get; set; }
    public List<string> PassengerIds { get; set; } = new();
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public Guid? ConversationId { get; set; }
    public List<LatLngDTO>? RoutePolylinePoints { get; set; }
}

public class CreateTripDTO
{
    public LatLngDTO Source { get; set; } = new();
    public LatLngDTO Target { get; set; } = new();
    public DateTime DepartureTime { get; set; }
    public int MaxDetourMeters { get; set; }
    public decimal PricePerSeat { get; set; }
    public int AvailableSeats { get; set; }
}

public class AddPassengerDTO
{
    public string PassengerId { get; set; } = string.Empty;
}

public class CreateTripRequestDTO
{
    public LatLngDTO Pickup  { get; set; } = new();
    public LatLngDTO Dropoff { get; set; } = new();
}

public class TripRequestDTO
{
    public string Id             { get; set; } = string.Empty;
    public string TripId         { get; set; } = string.Empty;
    public string RequesterId    { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public LatLngDTO Pickup      { get; set; } = new();
    public LatLngDTO Dropoff     { get; set; } = new();
    public List<LatLngDTO>? PreviewPolyline { get; set; }
    public int    DetourMeters   { get; set; }
    public string Status         { get; set; } = "PENDING";
}

public class PagedTripsDTO
{
    public List<TripDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
