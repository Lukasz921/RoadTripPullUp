using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MessageService.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageService.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", options => { });

            services.AddAuthorization();
            
            
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MessagesDbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<MessagesDbContext>(options =>
            {
                options.UseNpgsql(
                    "Host=localhost;Database=messages_db_test;Username=postgres;Password=admin");
            });
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();
            db.Database.EnsureDeleted();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    db.Database.Migrate();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {i + 1}: Failed to migrate database. Exception: {ex.Message}");
                    Thread.Sleep(2000); // wait before retrying
                }
            }
        });
    }
}