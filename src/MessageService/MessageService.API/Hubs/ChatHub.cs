using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MessageService.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public override Task OnConnectedAsync()
    {
        // client should call JoinConversation to join groups
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return base.OnDisconnectedAsync(exception);
    }

    public Task JoinConversation(string conversationId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public Task LeaveConversation(string conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
    }
}

