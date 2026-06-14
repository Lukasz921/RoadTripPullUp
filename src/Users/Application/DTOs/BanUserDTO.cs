namespace Users.Application.DTOs;

public record BanUserDTO
{
    public string Reason { get; init; } = string.Empty;
    public DateTime? Until { get; init; }
}
