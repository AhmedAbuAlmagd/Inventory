using System.Diagnostics;
using System.Security.Claims;
using SmartInventory.API.Infrastructure;

namespace SmartInventory.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        context.Items[HttpContextItemKeys.Stopwatch] = sw;
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            context.Items[HttpContextItemKeys.DurationMs] = sw.ElapsedMilliseconds;

            var requestId = context.Items.TryGetValue(HttpContextItemKeys.RequestId, out var ridObj)
                ? ridObj?.ToString()
                : context.TraceIdentifier;

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = context.User.Identity?.IsAuthenticated == true ? context.User.Identity?.Name : null;

            using (_logger.BeginScope(new Dictionary<string, object?>
                   {
                       ["RequestId"] = requestId,
                       ["Method"] = context.Request.Method,
                       ["Path"] = context.Request.Path.Value,
                       ["StatusCode"] = context.Response.StatusCode,
                       ["DurationMs"] = sw.ElapsedMilliseconds,
                       ["UserId"] = userId,
                       ["Username"] = username,
                       ["IP"] = context.Connection.RemoteIpAddress?.ToString()
                   }))
            {
                _logger.LogInformation("HTTP request completed");
            }
        }
    }
}
