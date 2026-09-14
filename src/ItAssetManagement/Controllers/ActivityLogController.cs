using ItAssetManagement.Constants;
using ItAssetManagement.Services;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

/// <summary>
/// The audit trail. Administrator-only: it names who did what, which is more than a
/// normal user needs to see about their colleagues.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class ActivityLogController(
    IActivityLogService activityLog,
    IUserService userService) : Controller
{
    // [FromQuery] is load-bearing, not decoration. The filter has a property called Action,
    // which collides with the {action} token of the default route, and the route value
    // provider is consulted before the query string. Without this the filter would bind the
    // literal string "Index" every time, fail to parse as an ActivityAction, and silently
    // leave the filter null so the page always showed everything.
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ActivityLogFilterViewModel filter, CancellationToken ct) =>
        View(new ActivityLogViewModel
        {
            Filter = filter,
            Results = await activityLog.GetPagedAsync(filter.AssetId, filter.Action, filter.Page, ct),
            AssetOptions = await userService.GetAssetOptionsAsync(ct)
        });
}
