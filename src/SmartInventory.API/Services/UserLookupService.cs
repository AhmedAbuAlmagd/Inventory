using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartInventory.API.Identity;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Services;

public class UserLookupService : IUserLookupService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserLookupService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetUsernamesByIdsAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<int, string>();

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, Username = u.UserName ?? "" })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(x => x.Id, x => x.Username);
    }
}

