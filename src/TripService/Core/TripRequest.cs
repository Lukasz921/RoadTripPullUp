namespace TripService.Core;

public class TripRequest
{
    public Guid     Id             { get; set; }
    public Guid     TripId         { get; set; }
    public Guid     RequesterId    { get; set; }
    public Guid     ConversationId { get; set; }
    public double   PickupLat      { get; set; }
    public double   PickupLng      { get; set; }
    public double   DropoffLat     { get; set; }
    public double   DropoffLng     { get; set; }
    public int      DetourMeters   { get; set; }
    public string   Status         { get; set; } = "PENDING";
    public DateTime CreatedAt      { get; set; }
    public DateTime? AcceptedAt    { get; set; }
}
