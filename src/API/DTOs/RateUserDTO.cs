namespace API.DTOs;

public record RateUserDTO
{
    public Guid UserId { get; init; }
    public int Value { get; init; }
    public string? Comment { get; init; }
}
