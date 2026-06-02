namespace MessageService.Application.Services;

public class ClockService : IClockService
{
    public DateTime Now => DateTime.UtcNow;
}