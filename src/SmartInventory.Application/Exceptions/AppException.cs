namespace SmartInventory.Application.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public object? Details { get; }

    protected AppException(string message, int statusCode, object? details = null) : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }
}
