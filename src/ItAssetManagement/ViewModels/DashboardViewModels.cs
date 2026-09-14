namespace ItAssetManagement.ViewModels;

public class DashboardViewModel
{
    public int TotalAssets { get; init; }
    public int AssignedCount { get; init; }
    public int UnassignedCount { get; init; }
    public int InRepairCount { get; init; }
    public int RetiredCount { get; init; }

    /// <summary>Assets whose warranty has already lapsed, which is what drives replacement budgeting.</summary>
    public int OutOfWarrantyCount { get; init; }

    public decimal TotalPurchaseValue { get; init; }

    public int OpenTicketCount { get; init; }

    public double AssignedPercentage =>
        TotalAssets == 0 ? 0 : Math.Round(AssignedCount * 100.0 / TotalAssets, 1);
}

/// <summary>One labelled value in a chart. Serialised straight to JSON for Chart.js.</summary>
public record ChartPoint(string Label, int Value);
