namespace Users.Application.DTOs;

public class UserIntegrationDTO
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TripIntegrationData Trip { get; set; } = new();
}

public class TripIntegrationData
{
    public bool IsAdult { get; set; }
    public bool CanCreateTrip { get; set; }
}
