namespace TripService.Application;

public class TripSummaryV1DTO
{
    public string Id { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public LatLngDTO Source { get; set; } = new();
    public LatLngDTO Target { get; set; } = new();
    public DateTime DepartureTime { get; set; }
    public decimal PricePerSeat { get; set; }
    public int AvailableSeats { get; set; }
    public int MaxDetourMeters { get; set; }
    public int ActualDetourMeters { get; set; }
}
