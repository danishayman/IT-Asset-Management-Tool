using System.ComponentModel.DataAnnotations;
using ItAssetManagement.Constants;
using ItAssetManagement.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ItAssetManagement.ViewModels;

public class UserListItemViewModel
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>How much hardware this person is currently holding.</summary>
    public int AssignedAssetCount { get; init; }

    public bool IsAdmin => Role == Roles.Admin;
}

public class UserListViewModel
{
    public required IReadOnlyList<UserListItemViewModel> Users { get; init; }

    /// <summary>The signed-in administrator, so the view can refuse to offer self-destructive actions.</summary>
    public required int CurrentUserId { get; init; }
}

/// <summary>Backs both Create and Edit. Id is 0 when creating.</summary>
public class UserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username may contain only letters, digits, dots, underscores and hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = Roles.User;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Required when creating. On edit, leaving it blank keeps the existing password, so an
    /// administrator can correct a name or role without resetting someone's credentials.
    /// </summary>
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    public bool IsEdit => Id != 0;

    public static IReadOnlyList<SelectListItem> RoleOptions =>
        Roles.All.Select(r => new SelectListItem(r, r)).ToList();
}

public class ActivityLogViewModel
{
    public required PagedResult<ActivityLogItemViewModel> Results { get; init; }
    public required ActivityLogFilterViewModel Filter { get; init; }
    public required IReadOnlyList<SelectListItem> AssetOptions { get; init; }
}

public class ActivityLogFilterViewModel
{
    [Display(Name = "Asset")]
    public int? AssetId { get; set; }

    [Display(Name = "Action")]
    public ActivityAction? Action { get; set; }

    public int Page { get; set; } = 1;

    public bool HasAnyFilter => AssetId is not null || Action is not null;

    public Dictionary<string, string?> ToRouteValues() => new()
    {
        ["AssetId"] = AssetId?.ToString(),
        ["Action"] = Action?.ToString()
    };
}
