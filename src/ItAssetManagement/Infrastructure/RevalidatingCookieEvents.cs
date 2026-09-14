using System.Security.Claims;
using ItAssetManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ItAssetManagement.Infrastructure;

/// <summary>
/// Re-checks on each request that the account behind the cookie is still active, so an
/// administrator deactivating a user takes effect on that user's very next request rather
/// than whenever their cookie happens to expire.
/// <para>
/// This costs one indexed primary-key lookup per request. At this scale that is
/// negligible; a system with heavy traffic would instead cache a security stamp and only
/// revalidate on an interval.
/// </para>
/// </summary>
public class RevalidatingCookieEvents(
    IAuthService authService,
    ILogger<RevalidatingCookieEvents> logger) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        // A cookie with no usable subject is treated as invalid rather than trusted.
        if (!int.TryParse(userIdClaim, out var userId))
        {
            await RejectAsync(context, "cookie carried no usable user id");
            return;
        }

        if (!await authService.IsStillActiveAsync(userId))
        {
            await RejectAsync(context, $"user {userId} is deactivated or no longer exists");
        }
    }

    private async Task RejectAsync(CookieValidatePrincipalContext context, string reason)
    {
        logger.LogInformation("Rejecting authentication cookie: {Reason}.", reason);

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
