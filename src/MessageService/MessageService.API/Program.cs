using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using MessageService.Infrastructure;
using FluentValidation;
using FluentValidation.AspNetCore;
using MessageService.API.Hubs;
using MessageService.Application.Services;
using MessageService.Core.RepositoryInterfaces;
using MessageService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageService.API
{
    public static class MessageServiceExtensions
    {
        private const string CorsPolicyName = "MessageServiceAllowFrontend";

        // Registers MessageService pieces into the host's IServiceCollection
        public static WebApplicationBuilder AddMessageService(this WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var services = builder.Services;

            // Controllers + JSON options
            services.AddControllers().AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            // FluentValidation
            services.AddFluentValidationAutoValidation();
            // use a type from this assembly to discover validators
            services.AddValidatorsFromAssemblyContaining(typeof(ChatHub));

            services.AddEndpointsApiExplorer();

            // DbContext
            var conn = configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=messages;Username=postgres;Password=postgres";
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(conn)
            );

            // Redis
            var redisCfg = configuration.GetValue<string>("Redis:Configuration") ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisCfg)
            );

            // SignalR
            services.AddSignalR();

            // Register the untyped IHubContext for RedisNotificationService
            services.AddScoped<IHubContext>(sp => (IHubContext)sp.GetRequiredService<IHubContext<ChatHub>>());

            // CORS - for the SignalR hub we need AllowCredentials (separate policy so it doesn't interfere with app-wide policies)
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // Ensure JWT bearer will extract access_token for SignalR websocket requests.
            // Use Configure so we don't override other JWT configuration done by the host — we only attach the OnMessageReceived handler.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Preserve any existing Events by wrapping them.
                var prev = options.Events;
                var wrapped = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/chat"))
                        {
                            context.Token = accessToken;
                        }

                        await prev.OnMessageReceived(context);
                    },
                    // wire-through other commonly used handlers to avoid losing previously configured behavior
                    OnAuthenticationFailed = async context => { await prev.OnAuthenticationFailed(context); },
                    OnTokenValidated = async context => { await prev.OnTokenValidated(context); }
                };

                options.Events = wrapped;
            });

            // DI for repositories and services
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IMessageService, MessageService.Application.Services.MessageService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<INotificationService, RedisNotificationService>();
            services.AddScoped<IClockService, ClockService>();

            return builder;
        }

        // Applies migrations, optional swagger mapping and maps the SignalR hub on the app
        public static WebApplication UseMessageService(this WebApplication app)
        {
            // Apply migrations at startup (optional, useful for dev)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            if (app.Environment.IsDevelopment())
            {
            }

            // Ensure routing is available (host may have already called UseRouting)
            app.UseRouting();

            // Map MessageService-specific endpoints. Controllers are mapped by the main app's MapControllers() call
            // Map the SignalR hub and require the dedicated CORS policy that allows credentials
            app.MapHub<ChatHub>("/hub/chat").RequireCors(CorsPolicyName);

            return app;
        }
    }
}
