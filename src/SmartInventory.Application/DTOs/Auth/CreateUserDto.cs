using SmartInventory.Domain.Enums;

namespace SmartInventory.Application.DTOs.Auth;

public class CreateUserDto
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public UserRole Role { get; set; }
}

