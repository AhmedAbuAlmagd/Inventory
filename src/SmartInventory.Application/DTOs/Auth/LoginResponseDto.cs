namespace SmartInventory.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Role { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
