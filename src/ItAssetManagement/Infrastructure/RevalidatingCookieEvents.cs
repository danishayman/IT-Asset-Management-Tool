using System.Security.Claims;
using ItAssetManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

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

    /// <summary>
    /// Returns a real 403 instead of the cookie handler's default redirect to a page that
    /// then answers 200. The status code is what an API client, a crawler or a log reader
    /// actually goes on, and a signed-in user being redirected to "sign in" is misleading
    /// anyway: their problem is their role, not their session. UseStatusCodePagesWithReExecute
    /// still renders the friendly Access denied page over the top of it.
    /// </summary>
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    private async Task RejectAsync(CookieValidatePrincipalContext context, string reason)
    {
        logger.LogInformation("Rejecting authentication cookie: {Reason}.", reason);

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
