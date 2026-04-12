namespace Application.DTOs;

public class TripDetailsDTO
{
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers { get; set; }
    public Core.Entities.TripStatus Status { get; set; }
    public RouteDTO Route { get; set; } = new RouteDTO();
    public List<Guid> PassengerIds { get; set; } = new List<Guid>();
}