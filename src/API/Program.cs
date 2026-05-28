using Users;
using Users.Infrastructure;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Application.Messages;
using Application.TripPlanner;
using Infrastructure.Messages;
using Infrastructure.TripPlanner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API.Middleware;
using MessageService.API; // add extension methods from MessageService project

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
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
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

// definicje dla controlerow
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<ITripRequestRepository, TripRequestRepository>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddSingleton<ITripsV1Service, MockTripsV1Service>();
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