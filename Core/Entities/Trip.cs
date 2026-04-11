using Core.Enums;

namespace Core.Entities;

public class Trip
{
    public Guid Id { get; set; }
    public required User DriverId { get; set; }
    public required Route Route{get; set;}
    public float Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers;
    public List<User> Passengers { get; set; } = new List<User>();
}