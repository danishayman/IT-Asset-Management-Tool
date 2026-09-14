namespace ItAssetManagement.Domain;

public class Asset
{
    public int Id { get; set; }

    /// <summary>Human-facing identifier stencilled on the hardware, e.g. "ITAM-0042".</summary>
    public string AssetTag { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string SerialNumber { get; set; } = string.Empty;

    public AssetStatus Status { get; set; } = AssetStatus.Available;

    /// <summary>Null when the asset is not currently issued to anyone.</summary>
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    // DateOnly rather than DateTime: these are calendar facts with no meaningful
    // time-of-day, so storing them as `date` sidesteps timezone drift entirely.
    public DateOnly PurchaseDate { get; set; }
    public DateOnly WarrantyExpiry { get; set; }

    public decimal PurchaseCost { get; set; }

    public string Location { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
