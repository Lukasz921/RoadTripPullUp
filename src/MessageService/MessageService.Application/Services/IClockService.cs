namespace MessageService.Application.Services;

public interface IClockService
{
    DateTime Now { get; }
}