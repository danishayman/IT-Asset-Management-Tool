using ItAssetManagement.Constants;
using ItAssetManagement.Infrastructure;
using ItAssetManagement.Services;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

/// <summary>Administrator-only. The role gate sits on the controller, so no action can be added without it.</summary>
[Authorize(Roles = Roles.Admin)]
public class UsersController(
    IUserService userService,
    IExcelExportService excelExport) : Controller
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new UserListViewModel
        {
            Users = await userService.GetAllAsync(ct),
            CurrentUserId = User.GetUserId()
        });

    [HttpGet]
    public IActionResult Create() => View(new UserFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "A password is required when creating a user.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await userService.CreateAsync(model, ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        this.Success($"User {model.Username} created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await userService.GetForEditAsync(id, ct);

        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await userService.UpdateAsync(model, User.GetUserId(), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        this.Success($"User {model.Username} updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await userService.SetActiveAsync(id, isActive, User.GetUserId(), ct);

        if (result.Succeeded)
        {
            this.Success(isActive ? "Account reactivated." : "Account deactivated.");
        }
        else
        {
            this.Error(result.Error!);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await userService.DeleteAsync(id, User.GetUserId(), ct);

        if (result.Succeeded)
        {
            this.Success("User deleted.");
        }
        else
        {
            this.Error(result.Error!);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var users = await userService.GetAllAsync(ct);
        var workbook = excelExport.BuildUserWorkbook(users);

        return File(workbook, ExcelContentType, excelExport.BuildFileName("Users"));
    }
}
