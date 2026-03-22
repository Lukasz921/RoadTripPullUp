namespace Application.DTOs;

public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public AuthUserDTO User { get; set; } = new();
}

public class AuthUserDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}