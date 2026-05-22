namespace SmartInventory.Application.Exceptions;

public sealed class LockedOutException : AppException
{
    public LockedOutException(string message, DateTime? lockoutEndsAtUtc)
        : base(message, 423, new { lockoutEndsAtUtc })
    {
    }
}

