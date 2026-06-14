namespace TripService.Core;

public class TripRating
{
    public Guid     Id          { get; set; }
    public Guid     TripId      { get; set; }
    public Guid     RaterUserId { get; set; }
    public Guid     RatedUserId { get; set; }
    public short    Rating      { get; set; }
    public DateTime CreatedAt   { get; set; }
}
