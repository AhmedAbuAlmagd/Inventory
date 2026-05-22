namespace SmartInventory.Application.Exceptions;

public sealed class InsufficientStockException : AppException
{
    public InsufficientStockException(string message) : base(message, 400)
    {
    }
}

