namespace ItAssetManagement.Domain;

public class Ticket
{
    public int Id { get; set; }

    public int RaisedByUserId { get; set; }
    public User RaisedByUser { get; set; } = null!;

    /// <summary>Optional: a ticket may describe a general problem rather than one machine.</summary>
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime CreatedAt { get; set; }
}
