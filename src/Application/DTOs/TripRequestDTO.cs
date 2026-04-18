namespace Application.DTOs;

public class TripRequestDTO
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid PassengerId { get; set; }
    public string Status { get; set; } = string.Empty;
}
