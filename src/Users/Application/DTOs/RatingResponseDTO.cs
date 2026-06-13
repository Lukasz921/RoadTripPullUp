namespace Users.Application.DTOs;

public record RatingResponseDTO
{
    public Guid Id { get; init; }
    public Guid RaterId { get; init; }
    public string? RaterName { get; init; } // Optional: maybe the frontend wants to know who rated
    public int Value { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
