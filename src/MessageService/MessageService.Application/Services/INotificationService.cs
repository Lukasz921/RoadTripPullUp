using MessageService.Core.Models;

namespace MessageService.Application.Services;

public interface INotificationService
{
    Task PublishMessageCreatedAsync(Message message);
    Task PublishMessagesReadAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid readerId, DateTime readAt);
}
