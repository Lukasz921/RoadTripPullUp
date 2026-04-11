using Core.Enums;

namespace Core.Entities;

public enum TripRequestStatus
{
    Pending,
    Rejected,
    Cancelled,
}


public class TripRequest
{
    public Guid Id { get; set; }
    public Guid PassengerId {get; set;}
    public Guid TripId {get; set;}
    public TripRequestStatus TripRequestStatus {get; set;} = TripRequestStatus.Pending;
}