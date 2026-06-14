namespace Users.Core;

public class Complaint
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid ComplainerId { get; set; }
    public Guid ComplainedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
