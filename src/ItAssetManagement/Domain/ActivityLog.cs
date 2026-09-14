namespace ItAssetManagement.Domain;

/// <summary>
/// An append-only audit row. One is written inside the same transaction as every
/// asset mutation, so the trail cannot drift out of step with the data.
/// </summary>
public class ActivityLog
{
    public int Id { get; set; }

    /// <summary>
    /// Nullable so that the audit trail outlives the asset it describes: deleting an
    /// asset sets this to null rather than cascading the history away. The asset tag is
    /// duplicated into <see cref="Details"/> so a deleted asset stays identifiable.
    /// </summary>
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }

    public int PerformedByUserId { get; set; }
    public User PerformedByUser { get; set; } = null!;

    public ActivityAction Action { get; set; }

    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
