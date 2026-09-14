using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ItAssetManagement.Services;

public interface IAssetService
{
    Task<PagedResult<AssetListItemViewModel>> GetPagedAsync(
        AssetFilterViewModel filter, CancellationToken ct = default);

    /// <summary>Every row matching the filter, unpaged, for the Excel export.</summary>
    Task<IReadOnlyList<AssetListItemViewModel>> GetAllMatchingAsync(
        AssetFilterViewModel filter, CancellationToken ct = default);

    Task<AssetDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default);

    Task<AssetFormViewModel?> GetForEditAsync(int id, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(AssetFormViewModel model, int actingUserId, CancellationToken ct = default);

    Task<ServiceResult> UpdateAsync(AssetFormViewModel model, int actingUserId, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int id, int actingUserId, CancellationToken ct = default);

    Task<ServiceResult> AssignAsync(int assetId, int assigneeUserId, int actingUserId, CancellationToken ct = default);

    Task<ServiceResult> UnassignAsync(int assetId, int actingUserId, CancellationToken ct = default);

    Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SelectListItem>> GetAssignableUserOptionsAsync(CancellationToken ct = default);
}
