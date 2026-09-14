namespace ItAssetManagement.Domain;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt hash. A plaintext password is never persisted, not even for seeded accounts.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>One of <see cref="Constants.Roles"/>. Stored as text so it can feed a role claim directly.</summary>
    public string Role { get; set; } = Constants.Roles.User;

    /// <summary>Deactivated users keep their history but cannot sign in.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Asset> AssignedAssets { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
