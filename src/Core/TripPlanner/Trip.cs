namespace Core.TripPlanner;

public enum TripStatus
{
    InActive, Active, Full, Cancelled, Done, Archived
}

public class Trip
{
    public Guid Id { get; set; }
    public required Guid DriverId { get; set; }
    public required Guid RouteId { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers { get; set; }
    public List<Guid> PassengerIds { get; private set; } = new();

    public TripStatus OfferStatus { get; set; } = TripStatus.InActive;

    public uint RowVersion { get; set; }

    public bool TryAddPassenger(Guid passengerId)
    {
        if (passengerId == Guid.Empty)
            return false;

        if (OfferStatus != TripStatus.Active)
            return false;

        if (PassengerIds.Count >= MaxPassengers)
            return false;

        if (passengerId == DriverId)
            return false;

        if (PassengerIds.Contains(passengerId))
            return false;

        PassengerIds.Add(passengerId);

        if (PassengerIds.Count >= MaxPassengers)
        {
            OfferStatus = TripStatus.Full;
        }

        return true;
    }
}
