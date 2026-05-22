using System.Diagnostics;
using FluentValidation;
using SmartInventory.API.Contracts;
using SmartInventory.API.Infrastructure;
using SmartInventory.Application.Exceptions;

namespace SmartInventory.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Application exception: {Message}", ex.Message);
            await WriteErrorAsync(context, ex.StatusCode, ex.Message, ex.Details);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogInformation("Validation failed");
            await WriteErrorAsync(context, 400, "Validation failed", errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, 500, "An unexpected error occurred. Please try again later.", null);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message, object? details)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var requestId = context.Items.TryGetValue(HttpContextItemKeys.RequestId, out var ridObj)
            ? ridObj?.ToString() ?? context.TraceIdentifier
            : context.TraceIdentifier;

        var durationMs = TryGetDurationMs(context);

        var response = ApiResponse<object>.Fail(
            new ApiError { Message = message, Details = details },
            statusCode,
            durationMs,
            requestId);

        await context.Response.WriteAsJsonAsync(response);
    }

    private static long TryGetDurationMs(HttpContext context)
    {
        if (context.Items.TryGetValue(HttpContextItemKeys.DurationMs, out var durationObj) && durationObj is long durationLong)
        {
            return durationLong;
        }

        if (context.Items.TryGetValue(HttpContextItemKeys.Stopwatch, out var swObj) && swObj is Stopwatch sw)
        {
            return sw.ElapsedMilliseconds;
        }

        return 0;
    }
}
