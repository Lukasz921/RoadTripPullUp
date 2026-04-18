namespace Core.Entities;

public enum TripStatus
{
    InActive, Active, Full, Cancelled, Done, Archived
}

public class Trip
{
    public Guid Id { get; set; }
    public required Guid DriverId { get; set; }
    public required Guid RouteId{get; set;}
    public float Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers {get; set;}
    public List<User> Passengers { get; set; } = new ();
    
    public TripStatus OfferStatus {get; set; } = TripStatus.InActive;
}