using ItAssetManagement.Data;
using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItAssetManagement.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardViewModel> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Grouping the whole table into a single bucket turns every tile below into one
        // set of conditional aggregates, so the page costs one query rather than seven.
        var summary = await db.Assets
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Assigned = g.Count(a => a.AssignedToUserId != null),
                Repair = g.Count(a => a.Status == AssetStatus.Repair),
                Retired = g.Count(a => a.Status == AssetStatus.Retired),
                OutOfWarranty = g.Count(a => a.WarrantyExpiry < today),
                Value = g.Sum(a => a.PurchaseCost)
            })
            .SingleOrDefaultAsync(ct);

        var openTickets = await db.Tickets.CountAsync(t => t.Status != TicketStatus.Closed, ct);

        // No assets at all means the grouped query returns no rows, not a row of zeroes.
        if (summary is null)
        {
            return new DashboardViewModel { OpenTicketCount = openTickets };
        }

        return new DashboardViewModel
        {
            TotalAssets = summary.Total,
            AssignedCount = summary.Assigned,
            UnassignedCount = summary.Total - summary.Assigned,
            InRepairCount = summary.Repair,
            RetiredCount = summary.Retired,
            OutOfWarrantyCount = summary.OutOfWarranty,
            TotalPurchaseValue = summary.Value,
            OpenTicketCount = openTickets
        };
    }

    public async Task<IReadOnlyList<ChartPoint>> GetStatusBreakdownAsync(CancellationToken ct = default)
    {
        var counts = await db.Assets
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var bySrc = counts.ToDictionary(c => c.Status, c => c.Count);

        // Every status is emitted, including the ones at zero, so the doughnut's colours
        // stay bound to the same slice as assets move between statuses.
        return Enum.GetValues<AssetStatus>()
            .Select(status => new ChartPoint(status.ToDisplayName(), bySrc.GetValueOrDefault(status)))
            .ToList();
    }

    public async Task<IReadOnlyList<ChartPoint>> GetCategoryBreakdownAsync(CancellationToken ct = default) =>
        await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new ChartPoint(c.Name, c.Assets.Count()))
            .ToListAsync(ct);
}
