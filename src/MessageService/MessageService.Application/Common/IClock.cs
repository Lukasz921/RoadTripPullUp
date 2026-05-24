namespace MessageService.Application.Common;

public interface IClock
{
    DateTime Now { get; }
}