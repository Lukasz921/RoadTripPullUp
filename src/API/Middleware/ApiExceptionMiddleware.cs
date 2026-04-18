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
        string title = "Error";
        string type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        if (exception is Application.Exceptions.ValidationException vex)
        {
            code = System.Net.HttpStatusCode.BadRequest;
            message = vex.Message;
            title = "Validation Error";
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        }
        else if (exception is Application.Exceptions.NotFoundException nf)
        {
            code = System.Net.HttpStatusCode.NotFound;
            message = nf.Message;
            title = "Not Found";
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        }
        else if (exception is Application.Exceptions.SeatUnavailableException suex)
        {
            code = System.Net.HttpStatusCode.Conflict;
            message = suex.Message;
            title = "Seat Unavailable";
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
        }
        else if (exception is InvalidOperationException ioe && exception.Message.Contains("concurrency conflict"))
        {
            code = System.Net.HttpStatusCode.Conflict;
            message = ioe.Message;
            title = "Concurrency Conflict";
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
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

        var payloadObject = new
        {
            type = type,
            title = title,
            status = (int)code,
            detail = message,
            stackTrace = details
        };

        var payload = JsonSerializer.Serialize(payloadObject, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(payload);
    }
}
