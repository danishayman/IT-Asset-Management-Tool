using System.Security.Claims;
using ItAssetManagement.Constants;
using ItAssetManagement.Data;
using ItAssetManagement.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ItAssetManagement.Services;

public class AuthService(AppDbContext db, IPasswordHasher hasher, ILogger<AuthService> logger) : IAuthService
{
    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            // Burn a real hashing round before giving up. Returning immediately would make an
            // unknown username measurably faster to reject than a wrong password, and that gap
            // is enough to enumerate valid accounts. Hash costs the same as Verify at a given
            // work factor, so this evens out the response time without needing a stored hash.
            _ = hasher.Hash(password);
            logger.LogWarning("Failed sign-in for unknown username {Username}.", username);
            return null;
        }

        if (!hasher.Verify(password, user.PasswordHash))
        {
            logger.LogWarning("Failed sign-in for {Username}: incorrect password.", username);
            return null;
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Blocked sign-in for {Username}: account is deactivated.", username);
            return null;
        }

        return user;
    }

    public ClaimsPrincipal BuildPrincipal(User user)
    {
        // The role goes in as a claim so [Authorize(Roles = ...)] works straight off the
        // cookie, with no database round trip on the authorisation path.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(AppClaimTypes.FullName, user.FullName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public Task<bool> IsStillActiveAsync(int userId) =>
        db.Users.AnyAsync(u => u.Id == userId && u.IsActive);
}
