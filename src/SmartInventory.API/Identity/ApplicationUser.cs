using Microsoft.AspNetCore.Identity;

namespace SmartInventory.API.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

