using Microsoft.EntityFrameworkCore;
using Steam.Application.Exceptions;

namespace Steam.Api.MiddleWares;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted && ex is not OperationCanceledException)
        {
            var (status, message) = ex switch
            {
                ArgumentException => (400, ex.Message),
                KeyNotFoundException => (404, ex.Message),
                UnauthorizedAccessException => (403, "Access is denied."),
                ConflictException => (409, ex.Message),
                PaymentUnavailableException => (503, ex.Message),
                DbUpdateException => (409, "The data conflicts with an existing record or relationship."),
                NotSupportedException => (501, ex.Message),
                _ => (500, "An unexpected server error occurred.")
            };
            if (status == 500)
                _logger.LogError(ex, "Request failed: {Path}", context.Request.Path);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { message });
        }
    }
}
