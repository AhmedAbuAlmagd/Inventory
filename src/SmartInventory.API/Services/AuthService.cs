using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartInventory.API.Data;
using SmartInventory.API.Identity;
using SmartInventory.Application.DTOs.Auth;
using SmartInventory.Application.Exceptions;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;

namespace SmartInventory.API.Services;

public class AuthService : IAuthService
{
    private const int RefreshTokenSizeBytes = 64;

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<int>> roleManager,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var username = dto.Username.Trim();
        var user = await _userManager.FindByNameAsync(username);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid username or password");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var ends = await _userManager.GetLockoutEndDateAsync(user);
            throw new LockedOutException("Account is locked. Try again later.", ends?.UtcDateTime);
        }

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (signIn.IsLockedOut)
        {
            var ends = await _userManager.GetLockoutEndDateAsync(user);
            throw new LockedOutException("Account is locked. Try again later.", ends?.UtcDateTime);
        }

        if (!signIn.Succeeded)
        {
            throw new UnauthorizedException("Invalid username or password");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Employee";

        var (token, expiresAtUtc) = CreateJwt(user, roles);
        var (refreshToken, refreshTokenExpiresAtUtc) = await CreateAndStoreRefreshTokenAsync(
            user.Id,
            createdByIp: null,
            userAgent: null,
            cancellationToken);

        return new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Username = user.UserName ?? username,
            Role = primaryRole,
            ExpiresAtUtc = expiresAtUtc,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
        };
    }

    public async Task<LoginResponseDto> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        var rawToken = dto.RefreshToken.Trim();
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        var tokenHash = ComputeSha256Hex(rawToken);

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing is null)
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        var now = DateTime.UtcNow;

        if (existing.RevokedAtUtc is not null)
        {
            if (!string.IsNullOrWhiteSpace(existing.ReplacedByTokenHash))
            {
                await RevokeAllRefreshTokensForUserAsync(existing.UserId, now, cancellationToken);
                throw new UnauthorizedException("Refresh token reuse detected");
            }

            throw new UnauthorizedException("Invalid refresh token");
        }

        if (existing.ExpiresAtUtc <= now)
        {
            throw new UnauthorizedException("Refresh token expired");
        }

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var ends = await _userManager.GetLockoutEndDateAsync(user);
            throw new LockedOutException("Account is locked. Try again later.", ends?.UtcDateTime);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Employee";

        var (jwt, jwtExpiresAtUtc) = CreateJwt(user, roles);

        var (newRefreshToken, newRefreshExpiresAtUtc, newRefreshHash) = CreateRefreshToken(now);

        existing.RevokedAtUtc = now;
        existing.ReplacedByTokenHash = newRefreshHash;
        existing.UpdatedAt = now;
        _db.RefreshTokens.Update(existing);

        await _db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshHash,
            ExpiresAtUtc = newRefreshExpiresAtUtc,
            CreatedByIp = existing.CreatedByIp,
            UserAgent = existing.UserAgent
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            Token = jwt,
            RefreshToken = newRefreshToken,
            Username = user.UserName ?? user.Id.ToString(),
            Role = primaryRole,
            ExpiresAtUtc = jwtExpiresAtUtc,
            RefreshTokenExpiresAtUtc = newRefreshExpiresAtUtc
        };
    }

    public async Task LogoutAsync(RefreshTokenRequestDto dto, int userId, CancellationToken cancellationToken = default)
    {
        var rawToken = dto.RefreshToken.Trim();
        if (string.IsNullOrWhiteSpace(rawToken)) return;

        var tokenHash = ComputeSha256Hex(rawToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (existing is null) return;
        if (existing.UserId != userId) throw new ForbiddenException("Refresh token does not belong to the current user");
        if (existing.RevokedAtUtc is not null) return;

        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.UpdatedAt = existing.RevokedAtUtc;
        _db.RefreshTokens.Update(existing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var username = dto.Username.Trim();
        if (await _userManager.FindByNameAsync(username) is not null)
        {
            throw new ConflictException("Username already exists");
        }

        var roleName = dto.Role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var createdRole = await _roleManager.CreateAsync(new IdentityRole<int>(roleName));
            if (!createdRole.Succeeded)
            {
                throw new ConflictException(string.Join("; ", createdRole.Errors.Select(e => e.Description)));
            }
        }

        var user = new ApplicationUser
        {
            UserName = username,
            IsActive = true
        };

        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
        {
            throw new ConflictException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        var addedToRole = await _userManager.AddToRoleAsync(user, roleName);
        if (!addedToRole.Succeeded)
        {
            throw new ConflictException(string.Join("; ", addedToRole.Errors.Select(e => e.Description)));
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? username,
            Role = roleName,
            IsActive = user.IsActive
        };
    }

    private (string Token, DateTime ExpiresAtUtc) CreateJwt(ApplicationUser user, IEnumerable<string> roles)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes");
        }

        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured");
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryHours"], out var h) ? h : 8;

        var now = DateTime.UtcNow;
        var expires = now.AddHours(expiryHours);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Id.ToString())
        };

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expires);
    }

    private async Task<(string RefreshToken, DateTime RefreshTokenExpiresAtUtc)> CreateAndStoreRefreshTokenAsync(
        int userId,
        string? createdByIp,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var (refreshToken, expiresAtUtc, tokenHash) = CreateRefreshToken(now);

        await _db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByIp = createdByIp,
            UserAgent = userAgent
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return (refreshToken, expiresAtUtc);
    }

    private (string RefreshToken, DateTime ExpiresAtUtc, string TokenHash) CreateRefreshToken(DateTime nowUtc)
    {
        var refreshDays = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var d) ? d : 7;
        if (refreshDays < 1) refreshDays = 7;

        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes);
        var token = Base64UrlEncode(bytes);
        var hash = ComputeSha256Hex(token);

        return (token, nowUtc.AddDays(refreshDays), hash);
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] data)
    {
        var s = Convert.ToBase64String(data);
        return s.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private async Task RevokeAllRefreshTokensForUserAsync(int userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.RevokedAtUtc = nowUtc;
            t.UpdatedAt = nowUtc;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
