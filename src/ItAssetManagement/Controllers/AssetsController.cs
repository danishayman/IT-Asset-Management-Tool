using ItAssetManagement.Constants;
using ItAssetManagement.Infrastructure;
using ItAssetManagement.Services;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

/// <summary>
/// Read access for any signed-in user; every mutation is restricted to administrators.
/// The controller stays thin: it binds, delegates to <see cref="IAssetService"/>, and
/// turns the result into a view or a redirect.
/// </summary>
[Authorize]
public class AssetsController(
    IAssetService assetService,
    IExcelExportService excelExport) : Controller
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AssetFilterViewModel filter, CancellationToken ct)
    {
        var model = new AssetListViewModel
        {
            Filter = filter,
            Results = await assetService.GetPagedAsync(filter, ct),
            CategoryOptions = await assetService.GetCategoryOptionsAsync(ct)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await assetService.GetDetailsAsync(id, ct);

        if (model is null)
        {
            return NotFound();
        }

        // Only administrators can assign, so only they need the list of people to assign to.
        if (User.IsAdmin())
        {
            model.AssignableUsers = await assetService.GetAssignableUserOptionsAsync(ct);
        }

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new AssetFormViewModel();
        await PopulateOptionsAsync(model, ct);

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetFormViewModel model, CancellationToken ct)
    {
        ValidateWarrantyAfterPurchase(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, ct);
            return View(model);
        }

        var result = await assetService.CreateAsync(model, User.GetUserId(), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await PopulateOptionsAsync(model, ct);
            return View(model);
        }

        this.Success($"Asset {model.AssetTag} created.");
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await assetService.GetForEditAsync(id, ct);

        if (model is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AssetFormViewModel model, CancellationToken ct)
    {
        ValidateWarrantyAfterPurchase(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, ct);
            return View(model);
        }

        var result = await assetService.UpdateAsync(model, User.GetUserId(), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await PopulateOptionsAsync(model, ct);
            return View(model);
        }

        this.Success($"Asset {model.AssetTag} updated.");
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // Deletion is POST-only: a GET that destroys data can be triggered by a crawler or a
    // prefetching browser. The confirmation dialog lives in the view.
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await assetService.DeleteAsync(id, User.GetUserId(), ct);

        if (result.Succeeded)
        {
            this.Success("Asset deleted.");
        }
        else
        {
            this.Error(result.Error!);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int assignedToUserId, CancellationToken ct)
    {
        var result = await assetService.AssignAsync(id, assignedToUserId, User.GetUserId(), ct);

        if (result.Succeeded)
        {
            this.Success("Asset assigned.");
        }
        else
        {
            this.Error(result.Error!);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int id, CancellationToken ct)
    {
        var result = await assetService.UnassignAsync(id, User.GetUserId(), ct);

        if (result.Succeeded)
        {
            this.Success("Asset returned to the available pool.");
        }
        else
        {
            this.Error(result.Error!);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Exports whatever the list is currently showing. It binds the same filter type as
    /// Index, so the export cannot drift out of step with what the user is looking at.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export([FromQuery] AssetFilterViewModel filter, CancellationToken ct)
    {
        var assets = await assetService.GetAllMatchingAsync(filter, ct);
        var workbook = excelExport.BuildAssetWorkbook(assets);

        return File(workbook, ExcelContentType, excelExport.BuildFileName("Assets"));
    }

    private async Task PopulateOptionsAsync(AssetFormViewModel model, CancellationToken ct)
    {
        // Dropdowns are not posted back, so they have to be refilled whenever the form is
        // redisplayed after a validation failure.
        model.CategoryOptions = await assetService.GetCategoryOptionsAsync(ct);
        model.UserOptions = await assetService.GetAssignableUserOptionsAsync(ct);
    }

    /// <summary>
    /// A cross-field rule, which DataAnnotations cannot express on a single property, so it
    /// is applied to ModelState directly before the validity check.
    /// </summary>
    private void ValidateWarrantyAfterPurchase(AssetFormViewModel model)
    {
        if (model.WarrantyExpiry < model.PurchaseDate)
        {
            ModelState.AddModelError(
                nameof(model.WarrantyExpiry),
                "Warranty expiry cannot be earlier than the purchase date.");
        }
    }
}
