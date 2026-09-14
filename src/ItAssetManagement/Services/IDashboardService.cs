using ItAssetManagement.ViewModels;

namespace ItAssetManagement.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetSummaryAsync(CancellationToken ct = default);

    /// <summary>Asset counts per status, for the doughnut chart.</summary>
    Task<IReadOnlyList<ChartPoint>> GetStatusBreakdownAsync(CancellationToken ct = default);

    /// <summary>Asset counts per category, for the bar chart.</summary>
    Task<IReadOnlyList<ChartPoint>> GetCategoryBreakdownAsync(CancellationToken ct = default);
}
