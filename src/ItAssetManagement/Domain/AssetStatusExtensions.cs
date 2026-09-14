namespace ItAssetManagement.Domain;

public static class AssetStatusExtensions
{
    /// <summary>
    /// How a status is spelled for people. Defined once so the table badge, the dashboard
    /// legend and the Excel export cannot drift apart, and so "InUse" never leaks into
    /// something handed to somebody outside IT.
    /// </summary>
    public static string ToDisplayName(this AssetStatus status) => status switch
    {
        AssetStatus.InUse => "In use",
        AssetStatus.Available => "Available",
        AssetStatus.Repair => "Repair",
        AssetStatus.Retired => "Retired",
        _ => status.ToString()
    };
}
