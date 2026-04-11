using Core.Enums;

namespace Core.Entities;


public enum OfferStatus
{
    Active, Full, Cancelled, Done, Archived
}

public class Offer
{
    public Guid Id { get; set; }
    public required Trip Trip { get; set; }
    public OfferStatus OfferStatus {get; set; } = OfferStatus.Active;
}