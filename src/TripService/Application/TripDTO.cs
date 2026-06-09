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

public class PagedTripsDTO
{
    public List<TripDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
