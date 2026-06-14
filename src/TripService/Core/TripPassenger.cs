namespace TripService.Core;

public class TripPassenger
{
    public Guid     TripId      { get; set; }
    public Guid     PassengerId { get; set; }
    public DateTime JoinedAt    { get; set; }
}
