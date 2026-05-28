namespace Users.Application.DTOs;

public class UpdateUserDTO
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Sex { get; set; }
}
