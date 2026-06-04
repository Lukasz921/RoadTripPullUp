using Users;
using Users.Infrastructure;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Application.Messages;
using Infrastructure.Messages;
using TripService.Api;
using TripService.Application;
using TripService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API.Middleware;
using StackExchange.Redis;
using MessageService.API; // add extension methods from MessageService project
using MessageService.API.Hubs; // add ChatHub for IHubContext

var builder = WebApplication.CreateBuilder(args);

// Register MessageService services (DbContext, SignalR, Redis, DI, validators, etc.)
builder.AddMessageService();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "RoadTripPullUp API";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        });
        return Task.CompletedTask;
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(TripV1Controller).Assembly)
    .AddApplicationPart(typeof(UsersModule).Assembly);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddUsersModule();

builder.Services.AddHttpClient("valhalla", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Valhalla:BaseUrl"] ?? "http://valhalla:8002");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
builder.Services.AddSingleton<IJobStore, RedisJobStore>();

builder.Services.AddScoped<IRoutingEngine, ValhallaRoutingEngine>();
builder.Services.AddScoped<IUserChecker, UserChecker>();
builder.Services.AddScoped<ITripsV1Service, TripsV1Service>();
builder.Services.AddScoped<ITripsSearchService, TripsSearchService>();
builder.Services.AddHostedService<SearchWorker>();
// Register Infrastructure DbContext (different from MessageService.Infrastructure.AppDbContext registered by AddMessageService)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessagingService, MessagingService>();

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// use exception middleware early to catch exceptions from downstream
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// wire message service (maps hub, applies migrations for message DB, enables message swagger in dev)
app.UseMessageService();

app.UseCors("AllowFrontend");
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    
    var userDbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    userDbContext.Database.Migrate();
}

app.MapControllers();

app.Run();

public partial class Program { }