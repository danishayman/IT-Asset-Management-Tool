using ItAssetManagement.Data;
using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItAssetManagement.Services;

public class TicketService(AppDbContext db, ILogger<TicketService> logger) : ITicketService
{
    public const int PageSize = 10;

    public async Task<PagedResult<TicketListItemViewModel>> GetPagedAsync(
        TicketFilterViewModel filter, int? restrictToUserId, CancellationToken ct = default)
    {
        var query = db.Tickets.AsNoTracking();

        // The ownership filter is applied in the query, not after it. Fetching everything
        // and trimming in memory would leak other people's tickets through the row count.
        if (restrictToUserId is not null)
        {
            query = query.Where(t => t.RaisedByUserId == restrictToUserId);
        }

        if (filter.Status is not null)
        {
            query = query.Where(t => t.Status == filter.Status);
        }

        var total = await query.CountAsync(ct);
        var safePage = Math.Max(1, filter.Page);

        var items = await query
            // Open work first, then newest. A closed ticket is rarely what someone opened
            // the page to find.
            .OrderBy(t => t.Status == TicketStatus.Closed)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((safePage - 1) * PageSize)
            .Take(PageSize)
            .Select(t => new TicketListItemViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Priority = t.Priority,
                Status = t.Status,
                RaisedByFullName = t.RaisedByUser.FullName,
                AssetId = t.AssetId,
                AssetTag = t.Asset != null ? t.Asset.AssetTag : null,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<TicketListItemViewModel>
        {
            Items = items,
            Page = safePage,
            PageSize = PageSize,
            TotalCount = total
        };
    }

    public Task<TicketDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default) =>
        db.Tickets
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TicketDetailsViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                RaisedByFullName = t.RaisedByUser.FullName,
                RaisedByUserId = t.RaisedByUserId,
                AssetId = t.AssetId,
                AssetTag = t.Asset != null ? t.Asset.AssetTag : null,
                AssetName = t.Asset != null ? t.Asset.Name : null,
                CreatedAt = t.CreatedAt
            })
            .SingleOrDefaultAsync(ct)!;

    public async Task<ServiceResult<int>> CreateAsync(
        TicketFormViewModel model, int raisedByUserId, CancellationToken ct = default)
    {
        // A tampered asset id would otherwise fail as a raw foreign-key violation.
        if (model.AssetId is not null && !await db.Assets.AnyAsync(a => a.Id == model.AssetId, ct))
        {
            return ServiceResult<int>.Fail("That asset does not exist.");
        }

        var ticket = new Ticket
        {
            RaisedByUserId = raisedByUserId,
            AssetId = model.AssetId,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Priority = model.Priority,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Ticket {TicketId} raised by user {UserId}.", ticket.Id, raisedByUserId);
        return ServiceResult<int>.Ok(ticket.Id);
    }

    public async Task<ServiceResult> ChangeStatusAsync(
        int id, TicketStatus status, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id, ct);

        if (ticket is null)
        {
            return ServiceResult.Fail("That ticket no longer exists.");
        }

        if (ticket.Status == status)
        {
            return ServiceResult.Fail($"This ticket is already {status.ToDisplayName()}.");
        }

        ticket.Status = status;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Ticket {TicketId} moved to {Status}.", id, status);
        return ServiceResult.Ok();
    }

    public Task<int> CountOpenAsync(int? restrictToUserId, CancellationToken ct = default)
    {
        var query = db.Tickets.Where(t => t.Status != TicketStatus.Closed);

        if (restrictToUserId is not null)
        {
            query = query.Where(t => t.RaisedByUserId == restrictToUserId);
        }

        return query.CountAsync(ct);
    }
}
