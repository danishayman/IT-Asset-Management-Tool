using ItAssetManagement.Constants;
using ItAssetManagement.Domain;
using ItAssetManagement.Infrastructure;
using ItAssetManagement.Services;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

/// <summary>
/// Anyone signed in can raise a ticket and see their own. Administrators see every
/// ticket and are the only ones who can move a ticket's status.
/// </summary>
[Authorize]
public class TicketsController(
    ITicketService ticketService,
    IUserService userService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] TicketFilterViewModel filter, CancellationToken ct)
    {
        var isAdmin = User.IsAdmin();

        return View(new TicketListViewModel
        {
            Filter = filter,
            Results = await ticketService.GetPagedAsync(filter, isAdmin ? null : User.GetUserId(), ct),
            ShowsAllUsers = isAdmin
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var ticket = await ticketService.GetDetailsAsync(id, ct);

        if (ticket is null)
        {
            return NotFound();
        }

        // Checked on the way out as well as in the list query: a normal user guessing an id
        // must not be able to read a colleague's ticket.
        if (!User.IsAdmin() && ticket.RaisedByUserId != User.GetUserId())
        {
            return Forbid();
        }

        return View(ticket);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new TicketFormViewModel
        {
            AssetOptions = await userService.GetAssetOptionsAsync(ct)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.AssetOptions = await userService.GetAssetOptionsAsync(ct);
            return View(model);
        }

        var result = await ticketService.CreateAsync(model, User.GetUserId(), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            model.AssetOptions = await userService.GetAssetOptionsAsync(ct);
            return View(model);
        }

        this.Success("Ticket raised. IT will pick it up from here.");
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, TicketStatus status, CancellationToken ct)
    {
        var result = await ticketService.ChangeStatusAsync(id, status, ct);

        if (result.Succeeded)
        {
            this.Success($"Ticket moved to {status.ToDisplayName()}.");
        }
        else
        {
            this.Error(result.Error!);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
