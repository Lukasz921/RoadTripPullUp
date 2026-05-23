using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using MessageService.Infrastructure;
using MessageService.Services;
using MessageService.Repositories;
using MessageService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Add services
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
var conn = configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=messages;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn)
);

// Redis
var redisCfg = configuration.GetValue<string>("Redis:Configuration") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisCfg)
);

// SignalR
builder.Services.AddSignalR();

// Auth (placeholder - integrate with main project's auth)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // configure as needed by main project
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context => Task.CompletedTask
        };
    });

// DI for repositories and services
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IMessageService, MessageService.Services.MessageService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<INotificationService, RedisNotificationService>();

var app = builder.Build();

// Apply migrations at startup (optional, useful for dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hub/chat");

app.Run();
