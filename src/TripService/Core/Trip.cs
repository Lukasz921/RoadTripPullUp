namespace TripService.Core;

public class Trip
{
    public Guid     Id             { get; set; }
    public Guid     DriverId       { get; set; }
    public double   SourceLat      { get; set; }
    public double   SourceLng      { get; set; }
    public double   TargetLat      { get; set; }
    public double   TargetLng      { get; set; }
    public int      RouteDistanceM { get; set; }
    public int      RouteDurationS { get; set; }
    public int      MaxDetourM     { get; set; }
    public DateTime DepartureTime  { get; set; }
    public decimal  PricePerSeat   { get; set; }
    public short    AvailableSeats { get; set; }
    public string   Status         { get; set; } = "ACTIVE";
    public DateTime CreatedAt      { get; set; }

    public List<TripPassenger> Passengers { get; set; } = new();
}
