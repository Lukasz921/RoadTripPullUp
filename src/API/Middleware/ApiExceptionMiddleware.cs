using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace API.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = System.Net.HttpStatusCode.InternalServerError;
        string message = "An unexpected error occurred.";
        string? details = null;

        if (exception is Application.Exceptions.ValidationException vex)
        {
            code = System.Net.HttpStatusCode.BadRequest;
            message = vex.Message;
        }
        else if (exception is Application.Exceptions.NotFoundException nf)
        {
            code = System.Net.HttpStatusCode.NotFound;
            message = nf.Message;
        }
        else
        {
            // other exceptions -> include details in Development
            if (_env.IsDevelopment())
            {
                message = exception.Message;
                details = exception.StackTrace;
            }
        }

        var payload = string.IsNullOrEmpty(details)
            ? JsonSerializer.Serialize(new { error = message })
            : JsonSerializer.Serialize(new { error = message, details });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(payload);
    }
}
