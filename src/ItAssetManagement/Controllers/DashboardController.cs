using ItAssetManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

[Authorize]
public class DashboardController(IDashboardService dashboard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await dashboard.GetSummaryAsync(ct));

    // The charts read their data from these endpoints rather than having it baked into
    // the Razor output, so the figures cannot go stale relative to the database and the
    // same data is available to anything else that wants it.
    [HttpGet]
    public async Task<IActionResult> StatusBreakdown(CancellationToken ct) =>
        Json(await dashboard.GetStatusBreakdownAsync(ct));

    [HttpGet]
    public async Task<IActionResult> CategoryBreakdown(CancellationToken ct) =>
        Json(await dashboard.GetCategoryBreakdownAsync(ct));
}
