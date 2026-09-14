using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;

namespace ItAssetManagement.Services;

public interface ITicketService
{
    /// <summary>
    /// A page of tickets. Pass <paramref name="restrictToUserId"/> for a normal user so they
    /// see only their own; pass null for an administrator, who sees every ticket.
    /// </summary>
    Task<PagedResult<TicketListItemViewModel>> GetPagedAsync(
        TicketFilterViewModel filter, int? restrictToUserId, CancellationToken ct = default);

    Task<TicketDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(TicketFormViewModel model, int raisedByUserId, CancellationToken ct = default);

    Task<ServiceResult> ChangeStatusAsync(int id, TicketStatus status, CancellationToken ct = default);

    Task<int> CountOpenAsync(int? restrictToUserId, CancellationToken ct = default);
}
