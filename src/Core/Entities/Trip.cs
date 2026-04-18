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
    public List<User> Passengers { get; private set; } = new ();
    
    public TripStatus OfferStatus {get; set; } = TripStatus.InActive;

    public uint RowVersion { get; set; }

    public bool TryAddPassenger(User passenger)
    {
        if (passenger == null)
            return false;

        if (OfferStatus != TripStatus.Active)
            return false;

        if (Passengers.Count >= MaxPassengers)
            return false;

        if (passenger.Id == DriverId)
            return false;

        if (Passengers.Any(p => p.Id == passenger.Id))
            return false;

        Passengers.Add(passenger);

        if (Passengers.Count >= MaxPassengers)
        {
            OfferStatus = TripStatus.Full;
        }

        return true;
    }
}