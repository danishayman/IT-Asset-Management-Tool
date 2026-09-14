using System.ComponentModel.DataAnnotations;
using ItAssetManagement.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ItAssetManagement.ViewModels;

/// <summary>
/// The criteria behind the asset list. The export action binds this same type, which is
/// what makes "export the current filtered list" structurally true rather than a second
/// implementation that has to be kept in step.
/// </summary>
public class AssetFilterViewModel
{
    /// <summary>Free text matched against name, asset tag and serial number.</summary>
    [Display(Name = "Search")]
    [StringLength(100)]
    public string? Search { get; set; }

    [Display(Name = "Status")]
    public AssetStatus? Status { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    public int Page { get; set; } = 1;

    public bool HasAnyFilter =>
        !string.IsNullOrWhiteSpace(Search) || Status is not null || CategoryId is not null;

    /// <summary>
    /// Filter values as route data, so paging links and the export button carry the
    /// current filter instead of silently resetting it.
    /// </summary>
    public Dictionary<string, string?> ToRouteValues() => new()
    {
        ["Search"] = Search,
        ["Status"] = Status?.ToString(),
        ["CategoryId"] = CategoryId?.ToString()
    };
}

public class AssetListItemViewModel
{
    public int Id { get; init; }
    public string AssetTag { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public AssetStatus Status { get; init; }
    public string? AssignedToFullName { get; init; }
    public string Location { get; init; } = string.Empty;
    public DateOnly WarrantyExpiry { get; init; }
}

public class AssetListViewModel
{
    public required AssetFilterViewModel Filter { get; init; }
    public required PagedResult<AssetListItemViewModel> Results { get; init; }
    public required IReadOnlyList<SelectListItem> CategoryOptions { get; init; }
}

public class AssetDetailsViewModel
{
    public int Id { get; init; }
    public string AssetTag { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public AssetStatus Status { get; init; }
    public int? AssignedToUserId { get; init; }
    public string? AssignedToFullName { get; init; }
    public DateOnly PurchaseDate { get; init; }
    public DateOnly WarrantyExpiry { get; init; }
    public decimal PurchaseCost { get; init; }
    public string Location { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public bool IsUnderWarranty => WarrantyExpiry >= DateOnly.FromDateTime(DateTime.UtcNow);

    public IReadOnlyList<ActivityLogItemViewModel> RecentActivity { get; set; } = [];

    /// <summary>Populated for administrators only, to back the assignment dropdown.</summary>
    public IReadOnlyList<SelectListItem> AssignableUsers { get; set; } = [];
}

/// <summary>Backs both Create and Edit. Id is 0 when creating.</summary>
public class AssetFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Asset tag is required.")]
    [StringLength(30, ErrorMessage = "Asset tag cannot exceed 30 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Asset tag may contain only letters, digits and hyphens.")]
    [Display(Name = "Asset tag")]
    public string AssetTag { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(120, ErrorMessage = "Name cannot exceed 120 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Serial number is required.")]
    [StringLength(60, ErrorMessage = "Serial number cannot exceed 60 characters.")]
    [Display(Name = "Serial number")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    public AssetStatus Status { get; set; } = AssetStatus.Available;

    [Display(Name = "Assigned to")]
    public int? AssignedToUserId { get; set; }

    [Required(ErrorMessage = "Purchase date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Purchase date")]
    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required(ErrorMessage = "Warranty expiry is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Warranty expiry")]
    public DateOnly WarrantyExpiry { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(3);

    [Range(0, 10_000_000, ErrorMessage = "Purchase cost must be between 0 and 10,000,000.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Purchase cost")]
    public decimal PurchaseCost { get; set; }

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(80, ErrorMessage = "Location cannot exceed 80 characters.")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// UpdatedAt of the row this form was rendered from, as ticks, carried in a hidden
    /// field. If someone else saved in the meantime this no longer matches and the update
    /// is refused rather than silently overwriting their work. Ticks rather than a
    /// formatted DateTime because a long round-trips through a form field exactly, with no
    /// culture or sub-second truncation to get wrong.
    /// </summary>
    public long RowVersion { get; set; }

    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> UserOptions { get; set; } = [];

    public bool IsEdit => Id != 0;
}

public class ActivityLogItemViewModel
{
    public int Id { get; init; }
    public int? AssetId { get; init; }
    public string? AssetTag { get; init; }
    public string PerformedByFullName { get; init; } = string.Empty;
    public ActivityAction Action { get; init; }
    public string Details { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
