using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ItAssetManagement.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserListItemViewModel>> GetAllAsync(CancellationToken ct = default);

    Task<UserFormViewModel?> GetForEditAsync(int id, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(UserFormViewModel model, CancellationToken ct = default);

    Task<ServiceResult> UpdateAsync(UserFormViewModel model, int actingUserId, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int id, int actingUserId, CancellationToken ct = default);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive, int actingUserId, CancellationToken ct = default);

    /// <summary>Assets for the activity log's filter dropdown.</summary>
    Task<IReadOnlyList<SelectListItem>> GetAssetOptionsAsync(CancellationToken ct = default);
}
