namespace Application.DTOs;

public class TripDetailsDTO
{
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers { get; set; }
    public TripStatusDTO Status { get; set; }
    public RouteDTO Route { get; set; } = new RouteDTO();
    public List<Guid> PassengerIds { get; set; } = new List<Guid>();
}

public enum TripStatusDTO
{
    InActive, Active, Full, Cancelled, Done, Archived
}

