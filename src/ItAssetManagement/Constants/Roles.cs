namespace ItAssetManagement.Constants;

/// <summary>
/// Role names. These are <c>const string</c> rather than an enum because
/// <c>[Authorize(Roles = ...)]</c> is an attribute argument and therefore needs a
/// compile-time constant. Everything else that is a fixed set of values uses an enum.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly IReadOnlyList<string> All = [Admin, User];
}
