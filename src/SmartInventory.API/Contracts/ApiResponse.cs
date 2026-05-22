namespace SmartInventory.API.Contracts;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int Status { get; init; }
    public long DurationMs { get; init; }
    public string RequestId { get; init; } = default!;
    public T? Data { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse<T> Ok(T? data, int status, long durationMs, string requestId) =>
        new()
        {
            Success = true,
            Status = status,
            DurationMs = durationMs,
            RequestId = requestId,
            Data = data
        };

    public static ApiResponse<T> Fail(ApiError error, int status, long durationMs, string requestId) =>
        new()
        {
            Success = false,
            Status = status,
            DurationMs = durationMs,
            RequestId = requestId,
            Error = error
        };
}

public sealed class ApiError
{
    public string Message { get; init; } = default!;
    public object? Details { get; init; }
}

