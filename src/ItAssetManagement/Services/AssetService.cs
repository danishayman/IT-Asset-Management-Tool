using ItAssetManagement.Data;
using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ItAssetManagement.Services;

public class AssetService(
    AppDbContext db,
    IActivityLogService activityLog,
    ILogger<AssetService> logger) : IAssetService
{
    public const int PageSize = 10;

    private const int RecentActivityCount = 10;

    /// <summary>Postgres SQLSTATE for unique_violation.</summary>
    private const string UniqueViolation = "23505";

    public async Task<PagedResult<AssetListItemViewModel>> GetPagedAsync(
        AssetFilterViewModel filter, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(filter);

        var total = await query.CountAsync(ct);
        var safePage = Math.Max(1, filter.Page);

        var items = await ProjectToListItem(query)
            .Skip((safePage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        return new PagedResult<AssetListItemViewModel>
        {
            Items = items,
            Page = safePage,
            PageSize = PageSize,
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<AssetListItemViewModel>> GetAllMatchingAsync(
        AssetFilterViewModel filter, CancellationToken ct = default) =>
        await ProjectToListItem(BuildFilteredQuery(filter)).ToListAsync(ct);

    public async Task<AssetDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        var details = await db.Assets
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AssetDetailsViewModel
            {
                Id = a.Id,
                AssetTag = a.AssetTag,
                Name = a.Name,
                CategoryName = a.Category.Name,
                SerialNumber = a.SerialNumber,
                Status = a.Status,
                AssignedToUserId = a.AssignedToUserId,
                AssignedToFullName = a.AssignedToUser != null ? a.AssignedToUser.FullName : null,
                PurchaseDate = a.PurchaseDate,
                WarrantyExpiry = a.WarrantyExpiry,
                PurchaseCost = a.PurchaseCost,
                Location = a.Location,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .SingleOrDefaultAsync(ct);

        if (details is null)
        {
            return null;
        }

        details.RecentActivity = await activityLog.GetRecentForAssetAsync(id, RecentActivityCount, ct);
        return details;
    }

    public async Task<AssetFormViewModel?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var asset = await db.Assets.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct);

        if (asset is null)
        {
            return null;
        }

        return new AssetFormViewModel
        {
            Id = asset.Id,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            CategoryId = asset.CategoryId,
            SerialNumber = asset.SerialNumber,
            Status = asset.Status,
            AssignedToUserId = asset.AssignedToUserId,
            PurchaseDate = asset.PurchaseDate,
            WarrantyExpiry = asset.WarrantyExpiry,
            PurchaseCost = asset.PurchaseCost,
            Location = asset.Location,
            RowVersion = asset.UpdatedAt.Ticks
        };
    }

    public async Task<ServiceResult<int>> CreateAsync(
        AssetFormViewModel model, int actingUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var asset = new Asset
        {
            AssetTag = model.AssetTag.Trim(),
            Name = model.Name.Trim(),
            CategoryId = model.CategoryId,
            SerialNumber = model.SerialNumber.Trim(),
            Status = model.Status,
            AssignedToUserId = NormaliseAssignee(model.Status, model.AssignedToUserId),
            PurchaseDate = model.PurchaseDate,
            WarrantyExpiry = model.WarrantyExpiry,
            PurchaseCost = model.PurchaseCost,
            Location = model.Location.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Assets.Add(asset);

        try
        {
            // Saved before the audit row so the asset has a real id to reference.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            db.Entry(asset).State = EntityState.Detached;
            return ServiceResult<int>.Fail($"Asset tag '{model.AssetTag}' is already in use.");
        }

        activityLog.Track(asset.Id, actingUserId, ActivityAction.Create,
            $"Created asset {asset.AssetTag} ({asset.Name}).");
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Asset {AssetTag} created by user {UserId}.", asset.AssetTag, actingUserId);
        return ServiceResult<int>.Ok(asset.Id);
    }

    public async Task<ServiceResult> UpdateAsync(
        AssetFormViewModel model, int actingUserId, CancellationToken ct = default)
    {
        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == model.Id, ct);

        if (asset is null)
        {
            return ServiceResult.Fail("That asset no longer exists.");
        }

        // Tell EF which revision the form was rendered from. If the row has moved on since,
        // the UPDATE's WHERE clause matches zero rows and EF raises a concurrency exception.
        db.Entry(asset).Property(a => a.UpdatedAt).OriginalValue =
            new DateTime(model.RowVersion, DateTimeKind.Utc);

        var changes = DescribeChanges(asset, model);

        asset.AssetTag = model.AssetTag.Trim();
        asset.Name = model.Name.Trim();
        asset.CategoryId = model.CategoryId;
        asset.SerialNumber = model.SerialNumber.Trim();
        asset.Status = model.Status;
        asset.AssignedToUserId = NormaliseAssignee(model.Status, model.AssignedToUserId);
        asset.PurchaseDate = model.PurchaseDate;
        asset.WarrantyExpiry = model.WarrantyExpiry;
        asset.PurchaseCost = model.PurchaseCost;
        asset.Location = model.Location.Trim();
        asset.UpdatedAt = DateTime.UtcNow;

        activityLog.Track(asset.Id, actingUserId, ActivityAction.Update,
            changes.Count > 0
                ? $"Updated {asset.AssetTag}: {string.Join("; ", changes)}."
                : $"Updated {asset.AssetTag} with no field changes.");

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail(
                "Someone else changed this asset while you were editing it. " +
                "Reopen the asset to see their changes, then apply yours again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return ServiceResult.Fail($"Asset tag '{model.AssetTag}' is already in use.");
        }

        logger.LogInformation("Asset {AssetTag} updated by user {UserId}.", asset.AssetTag, actingUserId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, int actingUserId, CancellationToken ct = default)
    {
        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == id, ct);

        if (asset is null)
        {
            return ServiceResult.Fail("That asset no longer exists.");
        }

        // Recorded with the tag inline, because the FK is set to null once the asset goes
        // and the tag is then the only way to tell which asset this row describes.
        activityLog.Track(asset.Id, actingUserId, ActivityAction.Delete,
            $"Deleted asset {asset.AssetTag} ({asset.Name}).");

        db.Assets.Remove(asset);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Asset {AssetTag} deleted by user {UserId}.", asset.AssetTag, actingUserId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AssignAsync(
        int assetId, int assigneeUserId, int actingUserId, CancellationToken ct = default)
    {
        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == assetId, ct);

        if (asset is null)
        {
            return ServiceResult.Fail("That asset no longer exists.");
        }

        var assignee = await db.Users
            .Where(u => u.Id == assigneeUserId && u.IsActive)
            .Select(u => new { u.Id, u.FullName })
            .SingleOrDefaultAsync(ct);

        if (assignee is null)
        {
            return ServiceResult.Fail("That user does not exist or is deactivated.");
        }

        asset.AssignedToUserId = assignee.Id;

        // Assigning hardware is what puts it in use; leaving it Available while somebody
        // holds it would make the dashboard counts lie.
        asset.Status = AssetStatus.InUse;
        asset.UpdatedAt = DateTime.UtcNow;

        activityLog.Track(asset.Id, actingUserId, ActivityAction.Assign,
            $"Assigned {asset.AssetTag} to {assignee.FullName}.");

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnassignAsync(int assetId, int actingUserId, CancellationToken ct = default)
    {
        var asset = await db.Assets
            .Include(a => a.AssignedToUser)
            .SingleOrDefaultAsync(a => a.Id == assetId, ct);

        if (asset is null)
        {
            return ServiceResult.Fail("That asset no longer exists.");
        }

        if (asset.AssignedToUserId is null)
        {
            return ServiceResult.Fail("That asset is not currently assigned to anyone.");
        }

        var previousHolder = asset.AssignedToUser?.FullName ?? "an unknown user";

        asset.AssignedToUserId = null;
        asset.Status = AssetStatus.Available;
        asset.UpdatedAt = DateTime.UtcNow;

        activityLog.Track(asset.Id, actingUserId, ActivityAction.Unassign,
            $"Returned {asset.AssetTag} from {previousHolder} to the available pool.");

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(CancellationToken ct = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SelectListItem>> GetAssignableUserOptionsAsync(CancellationToken ct = default) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectListItem($"{u.FullName} ({u.Username})", u.Id.ToString()))
            .ToListAsync(ct);

    private IQueryable<Asset> BuildFilteredQuery(AssetFilterViewModel filter)
    {
        var query = db.Assets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // ILIKE is Postgres's native case-insensitive match. Lowering both sides in C#
            // instead would defeat any index and force the comparison client-side.
            var term = $"%{filter.Search.Trim()}%";

            query = query.Where(a =>
                EF.Functions.ILike(a.Name, term) ||
                EF.Functions.ILike(a.AssetTag, term) ||
                EF.Functions.ILike(a.SerialNumber, term));
        }

        if (filter.Status is not null)
        {
            query = query.Where(a => a.Status == filter.Status);
        }

        if (filter.CategoryId is not null)
        {
            query = query.Where(a => a.CategoryId == filter.CategoryId);
        }

        return query;
    }

    /// <summary>
    /// Projects in the database so the query selects only the columns the list shows,
    /// rather than materialising whole entities and discarding most of each one.
    /// </summary>
    private static IQueryable<AssetListItemViewModel> ProjectToListItem(IQueryable<Asset> query) =>
        query
            .OrderBy(a => a.AssetTag)
            .Select(a => new AssetListItemViewModel
            {
                Id = a.Id,
                AssetTag = a.AssetTag,
                Name = a.Name,
                CategoryName = a.Category.Name,
                SerialNumber = a.SerialNumber,
                Status = a.Status,
                AssignedToFullName = a.AssignedToUser != null ? a.AssignedToUser.FullName : null,
                Location = a.Location,
                WarrantyExpiry = a.WarrantyExpiry
            });

    /// <summary>
    /// Anything not in use is by definition held by nobody, so the assignee is cleared
    /// rather than left pointing at someone who no longer has it.
    /// </summary>
    private static int? NormaliseAssignee(AssetStatus status, int? assignedToUserId) =>
        status == AssetStatus.InUse ? assignedToUserId : null;

    private static List<string> DescribeChanges(Asset current, AssetFormViewModel updated)
    {
        var changes = new List<string>();

        void Compare<T>(string field, T before, T after)
        {
            if (!EqualityComparer<T>.Default.Equals(before, after))
            {
                changes.Add($"{field} '{before}' to '{after}'");
            }
        }

        Compare("tag", current.AssetTag, updated.AssetTag.Trim());
        Compare("name", current.Name, updated.Name.Trim());
        Compare("serial", current.SerialNumber, updated.SerialNumber.Trim());
        Compare("status", current.Status, updated.Status);
        Compare("location", current.Location, updated.Location.Trim());
        Compare("cost", current.PurchaseCost, updated.PurchaseCost);

        return changes;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: UniqueViolation };
}
