using System.Security.Claims;
using ItAssetManagement.Constants;

namespace ItAssetManagement.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in user's id. Throws when absent, because every caller sits behind
    /// [Authorize]: a missing id there means the pipeline is misconfigured, and failing
    /// loudly beats attributing someone's changes to user 0 in the audit trail.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated principal has no usable user id claim.");
    }

    public static string GetFullName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(AppClaimTypes.FullName) ?? principal.Identity?.Name ?? "Unknown";

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(Roles.Admin);
}
