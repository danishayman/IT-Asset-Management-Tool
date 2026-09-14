using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;

namespace ItAssetManagement.Services;

public interface IActivityLogService
{
    /// <summary>
    /// Stages an audit row on the tracked context without saving. The caller saves it in
    /// the same SaveChangesAsync as the mutation it describes, so the two either both land
    /// or both roll back.
    /// </summary>
    void Track(int? assetId, int performedByUserId, ActivityAction action, string details);

    Task<PagedResult<ActivityLogItemViewModel>> GetPagedAsync(
        int? assetId, ActivityAction? action, int page, CancellationToken ct = default);

    Task<IReadOnlyList<ActivityLogItemViewModel>> GetRecentForAssetAsync(
        int assetId, int take, CancellationToken ct = default);
}
