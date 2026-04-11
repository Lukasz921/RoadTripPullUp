namespace Application.DTOs;

public class CreateTripResponseDTO
{
    public Guid TripId { get; set; }
    public Guid RouteId { get; set; }
    public float Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers { get; set; }
}
