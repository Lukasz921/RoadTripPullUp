namespace MessageService.IntegrationTests;

public class IntegrationTestCommon
{
    public async Task<Guid> CreateConversation2Members()
    {
        return await Task.FromResult(Guid.NewGuid());
    }
    
    public async Task<Guid> CreateConversation0Members()
    {
        return await Task.FromResult(Guid.NewGuid());
    }
    
    public async Task<Guid> CreateMessage(Guid conversationId)
    {
        return await Task.FromResult(Guid.NewGuid());
    }
}