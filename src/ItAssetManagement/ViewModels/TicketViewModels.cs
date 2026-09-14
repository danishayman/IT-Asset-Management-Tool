using System.ComponentModel.DataAnnotations;
using ItAssetManagement.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ItAssetManagement.ViewModels;

public class TicketListItemViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public TicketPriority Priority { get; init; }
    public TicketStatus Status { get; init; }
    public string RaisedByFullName { get; init; } = string.Empty;
    public int? AssetId { get; init; }
    public string? AssetTag { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class TicketListViewModel
{
    public required PagedResult<TicketListItemViewModel> Results { get; init; }
    public required TicketFilterViewModel Filter { get; init; }

    /// <summary>
    /// True when the viewer is an administrator, who sees every ticket. A normal user sees
    /// only their own, so the list needs to say which of the two it is showing.
    /// </summary>
    public required bool ShowsAllUsers { get; init; }
}

public class TicketFilterViewModel
{
    [Display(Name = "Status")]
    public TicketStatus? Status { get; set; }

    public int Page { get; set; } = 1;

    public bool HasAnyFilter => Status is not null;

    public Dictionary<string, string?> ToRouteValues() => new()
    {
        ["Status"] = Status?.ToString()
    };
}

public class TicketFormViewModel
{
    [Required(ErrorMessage = "A title is required.")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the problem.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters.")]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    /// <summary>Optional: a ticket may describe a general problem rather than one machine.</summary>
    [Display(Name = "Related asset")]
    public int? AssetId { get; set; }

    public IReadOnlyList<SelectListItem> AssetOptions { get; set; } = [];
}

public class TicketDetailsViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TicketPriority Priority { get; init; }
    public TicketStatus Status { get; init; }
    public string RaisedByFullName { get; init; } = string.Empty;
    public int RaisedByUserId { get; init; }
    public int? AssetId { get; init; }
    public string? AssetTag { get; init; }
    public string? AssetName { get; init; }
    public DateTime CreatedAt { get; init; }
}
