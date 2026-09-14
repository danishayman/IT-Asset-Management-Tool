using ItAssetManagement.Data;
using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItAssetManagement.Services;

public class ActivityLogService(AppDbContext db) : IActivityLogService
{
    public const int PageSize = 20;

    private const int MaxDetailsLength = 500;

    public void Track(int? assetId, int performedByUserId, ActivityAction action, string details)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            AssetId = assetId,
            PerformedByUserId = performedByUserId,
            Action = action,
            // Truncate rather than let an over-long detail string fail the whole save. The
            // audit row exists to describe the mutation; it must never be what blocks it.
            Details = details.Length > MaxDetailsLength ? details[..MaxDetailsLength] : details,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task<PagedResult<ActivityLogItemViewModel>> GetPagedAsync(
        int? assetId, ActivityAction? action, int page, CancellationToken ct = default)
    {
        var query = db.ActivityLogs.AsNoTracking();

        if (assetId is not null)
        {
            query = query.Where(l => l.AssetId == assetId);
        }

        if (action is not null)
        {
            query = query.Where(l => l.Action == action);
        }

        var total = await query.CountAsync(ct);
        var safePage = Math.Max(1, page);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .ThenByDescending(l => l.Id)
            .Skip((safePage - 1) * PageSize)
            .Take(PageSize)
            .Select(l => new ActivityLogItemViewModel
            {
                Id = l.Id,
                AssetId = l.AssetId,
                AssetTag = l.Asset != null ? l.Asset.AssetTag : null,
                PerformedByFullName = l.PerformedByUser.FullName,
                Action = l.Action,
                Details = l.Details,
                Timestamp = l.Timestamp
            })
            .ToListAsync(ct);

        return new PagedResult<ActivityLogItemViewModel>
        {
            Items = items,
            Page = safePage,
            PageSize = PageSize,
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<ActivityLogItemViewModel>> GetRecentForAssetAsync(
        int assetId, int take, CancellationToken ct = default) =>
        await db.ActivityLogs
            .AsNoTracking()
            .Where(l => l.AssetId == assetId)
            .OrderByDescending(l => l.Timestamp)
            .ThenByDescending(l => l.Id)
            .Take(take)
            .Select(l => new ActivityLogItemViewModel
            {
                Id = l.Id,
                AssetId = l.AssetId,
                AssetTag = l.Asset != null ? l.Asset.AssetTag : null,
                PerformedByFullName = l.PerformedByUser.FullName,
                Action = l.Action,
                Details = l.Details,
                Timestamp = l.Timestamp
            })
            .ToListAsync(ct);
}
