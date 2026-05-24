using System.Text.Json;
using MessageService.Core.Models;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace MessageService.Application.Services;

public class RedisNotificationService : INotificationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext _hub;
    private readonly IDatabase _db;

    public RedisNotificationService(IConnectionMultiplexer redis, IHubContext hub)
    {
        _redis = redis;
        _hub = hub;
        _db = redis.GetDatabase();
    }

    public async Task PublishMessageCreatedAsync(Message message)
    {
        var payload = new
        {
            eventType = "message.created",
            data = new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                senderId = message.SenderId,
                type = message.Type,
                payload = message.Payload,
                createdAt = message.CreatedAt
            }
        };

        var json = JsonSerializer.Serialize(payload);

        // publish to conversation channel
        var channel = $"channel:messages:conversation:{message.ConversationId}";
        await _redis.GetSubscriber().PublishAsync(channel, json);

        // broadcast to SignalR group for conversation
        await _hub.Clients.Group(message.ConversationId.ToString()).SendAsync("MessageCreated", payload);
    }

    public async Task PublishMessagesReadAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid readerId, DateTime readAt)
    {
        var payload = new
        {
            eventType = "message.read",
            data = new
            {
                conversationId,
                messageIds,
                readerId,
                readAt
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var channel = $"channel:messages:conversation:{conversationId}";
        await _redis.GetSubscriber().PublishAsync(channel, json);

        await _hub.Clients.Group(conversationId.ToString()).SendAsync("MessagesRead", payload);
    }
}
