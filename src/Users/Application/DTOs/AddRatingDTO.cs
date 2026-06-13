namespace Users.Application.DTOs;

public record AddRatingDTO
{
    public Guid UserId { get; init; }
    public Guid RaterId { get; init; }
    public int Value { get; init; }
    public string? Comment { get; init; }
}
