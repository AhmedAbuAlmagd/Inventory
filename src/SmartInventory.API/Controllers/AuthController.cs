using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using SmartInventory.API.Contracts;
using SmartInventory.Application.DTOs.Auth;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT access token and refresh token.
    /// </summary>
    /// <remarks>
    /// The refresh token is also stored in an HttpOnly cookie so Swagger and browser clients can refresh without manually copying tokens.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 423)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        SetRefreshTokenCookie(result);
        return Ok(result);
    }

    /// <summary>
    /// Exchanges a refresh token for a new JWT access token (and rotates the refresh token).
    /// </summary>
    /// <remarks>
    /// If the request body does not include a refresh token, the API will use the HttpOnly cookie if present.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 423)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        var token = string.IsNullOrWhiteSpace(dto.RefreshToken) ? GetRefreshTokenCookie() : dto.RefreshToken;
        var result = await _authService.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = token ?? "" }, cancellationToken);
        SetRefreshTokenCookie(result);
        return Ok(result);
    }

    /// <summary>
    /// Revokes the current refresh token (logout).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(raw, out var userId))
        {
            return Unauthorized();
        }

        var token = string.IsNullOrWhiteSpace(dto.RefreshToken) ? GetRefreshTokenCookie() : dto.RefreshToken;
        await _authService.LogoutAsync(new RefreshTokenRequestDto { RefreshToken = token ?? "" }, userId, cancellationToken);
        ClearRefreshTokenCookie();
        return Ok(null);
    }

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.CreateUserAsync(dto, cancellationToken);
        return StatusCode(201, result);
    }

    private void SetRefreshTokenCookie(LoginResponseDto dto)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Expires = dto.RefreshTokenExpiresAtUtc
        };

        Response.Cookies.Append(RefreshTokenCookieName, dto.RefreshToken, options);
    }

    private string? GetRefreshTokenCookie()
    {
        return Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token) ? token : null;
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName);
    }
}
