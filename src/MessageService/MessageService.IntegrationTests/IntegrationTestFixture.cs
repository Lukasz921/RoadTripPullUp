using MessageService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageService.IntegrationTests;

public class IntegrationTestFixture : IDisposable
{
    public CustomWebApplicationFactory Factory { get; }
    public HttpClient Client { get; }
    
    public IntegrationTestFixture()
    {
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();

        ResetDatabase();
    }
    
    private void ResetDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
    }
    
    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}