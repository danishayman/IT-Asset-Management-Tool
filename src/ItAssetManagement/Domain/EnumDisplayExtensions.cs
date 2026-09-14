namespace ItAssetManagement.Domain;

/// <summary>
/// How the enums are spelled for people. Defined once so the table badges, the dashboard
/// legend and the Excel exports cannot drift apart, and so a raw name like "InUse" never
/// leaks into something handed to somebody outside IT.
/// </summary>
public static class EnumDisplayExtensions
{
    public static string ToDisplayName(this AssetStatus status) => status switch
    {
        AssetStatus.InUse => "In use",
        AssetStatus.Available => "Available",
        AssetStatus.Repair => "Repair",
        AssetStatus.Retired => "Retired",
        _ => status.ToString()
    };

    public static string ToDisplayName(this TicketStatus status) => status switch
    {
        TicketStatus.Open => "Open",
        TicketStatus.InProgress => "In progress",
        TicketStatus.Closed => "Closed",
        _ => status.ToString()
    };

    public static string ToDisplayName(this TicketPriority priority) => priority.ToString();
}
