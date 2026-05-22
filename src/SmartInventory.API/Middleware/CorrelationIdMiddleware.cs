using SmartInventory.API.Infrastructure;

namespace SmartInventory.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Request-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
                        !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString().Trim()
            : Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemKeys.RequestId] = requestId;
        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (_logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = requestId }))
        {
            await _next(context);
        }
    }
}
