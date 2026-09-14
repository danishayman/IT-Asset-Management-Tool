using System.Security.Claims;
using ItAssetManagement.Domain;

namespace ItAssetManagement.Services;

public interface IAuthService
{
    /// <summary>
    /// Returns the user when the username exists, the account is active, and the password
    /// verifies. Returns null in every other case, deliberately without distinguishing
    /// which check failed.
    /// </summary>
    Task<User?> ValidateCredentialsAsync(string username, string password);

    ClaimsPrincipal BuildPrincipal(User user);

    /// <summary>Whether the account behind an existing cookie is still allowed to be signed in.</summary>
    Task<bool> IsStillActiveAsync(int userId);
}
