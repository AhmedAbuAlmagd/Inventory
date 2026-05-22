using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartInventory.API.Contracts;
using SmartInventory.API.Infrastructure;

namespace SmartInventory.API.Filters;

public class ApiResponseFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is FileResult or PhysicalFileResult)
        {
            return next();
        }

        var requestId = context.HttpContext.Items.TryGetValue(HttpContextItemKeys.RequestId, out var ridObj)
            ? ridObj?.ToString() ?? context.HttpContext.TraceIdentifier
            : context.HttpContext.TraceIdentifier;

        var durationMs = TryGetDurationMs(context.HttpContext);

        if (context.Result is ObjectResult objectResult)
        {
            var status = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
            if (status == 0) status = 200;

            if (status >= 400)
            {
                var error = new ApiError { Message = "Request failed", Details = objectResult.Value };
                context.Result = new ObjectResult(ApiResponse<object>.Fail(error, status, durationMs, requestId))
                {
                    StatusCode = status
                };
                return next();
            }

            context.Result = new ObjectResult(ApiResponse<object>.Ok(objectResult.Value, status, durationMs, requestId))
            {
                StatusCode = status
            };
            return next();
        }

        if (context.Result is StatusCodeResult statusCodeResult)
        {
            var status = statusCodeResult.StatusCode;
            if (status >= 400)
            {
                var error = new ApiError { Message = "Request failed" };
                context.Result = new ObjectResult(ApiResponse<object>.Fail(error, status, durationMs, requestId))
                {
                    StatusCode = status
                };
                return next();
            }

            context.Result = new ObjectResult(ApiResponse<object>.Ok(null, status, durationMs, requestId))
            {
                StatusCode = status
            };
            return next();
        }

        return next();
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

