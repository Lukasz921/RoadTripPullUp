using MessageService.Models;

namespace MessageService.Services;

public interface INotificationService
{
    Task PublishMessageCreatedAsync(Message message);
    Task PublishMessagesReadAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid readerId, DateTime readAt);
}
