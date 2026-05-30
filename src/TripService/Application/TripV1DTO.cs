namespace TripService.Application;

public class TripV1DTO
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
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
}

public class CreateTripV1DTO
{
    public LatLngDTO Source { get; set; } = new();
    public LatLngDTO Target { get; set; } = new();
    public DateTime DepartureTime { get; set; }
    public int MaxDetourMeters { get; set; }
    public decimal PricePerSeat { get; set; }
    public int AvailableSeats { get; set; }
}

public class MyTripsV1ResultDTO
{
    public List<TripV1DTO> Items { get; set; } = new();
    public int Count { get; set; }
}
