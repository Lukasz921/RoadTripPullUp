namespace Users.Application.DTOs;

public record ComplaintResponseDTO
{
    public Guid Id { get; init; }
    public Guid TripId { get; init; }
    public Guid ComplainerId { get; init; }
    public Guid ComplainedUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
