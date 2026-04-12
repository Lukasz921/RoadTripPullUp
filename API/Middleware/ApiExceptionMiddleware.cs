using System.Text.Json;

namespace API.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = System.Net.HttpStatusCode.InternalServerError;
        string message = "An unexpected error occurred.";

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

        var result = JsonSerializer.Serialize(new { error = message });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}
