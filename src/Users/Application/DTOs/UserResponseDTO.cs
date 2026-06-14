namespace Users.Application.DTOs;

public class UserResponseDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Sex { get; init; } = string.Empty;
    public double AvgRating { get; init; }
    public int RatingsCount { get; init; }
    public bool IsBanned { get; init; }
    public string? BanReason { get; init; }
    public DateTime? BannedUntil { get; init; }

    public bool IsCurrentlyBanned() => IsBanned && (BannedUntil == null || BannedUntil > DateTime.UtcNow);
}
