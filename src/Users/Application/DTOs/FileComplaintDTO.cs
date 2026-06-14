namespace Users.Application.DTOs;

public record FileComplaintDTO
{
    public Guid ComplainedUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
