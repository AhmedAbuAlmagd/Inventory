namespace SmartInventory.Application.Interfaces;

public interface IUserLookupService
{
    Task<IReadOnlyDictionary<int, string>> GetUsernamesByIdsAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default);
}

