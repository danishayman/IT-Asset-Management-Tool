using ItAssetManagement.Services;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

// Anonymous access is granted per action rather than on the controller. A controller-wide
// [AllowAnonymous] would silently override the [Authorize] on Logout, leaving sign-out
// reachable by anyone, and would keep doing so if a global authorisation policy were added.
public class AccountController(IAuthService authService, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Already signed in? Sending them back to the login form is just confusing.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocalOrHome(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await authService.ValidateCredentialsAsync(model.Username, model.Password);

        if (user is null)
        {
            // One message for every failure mode. Saying "no such user" or "account
            // disabled" would tell an attacker which usernames are real.
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var principal = authService.BuildPrincipal(user);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        logger.LogInformation("User {Username} signed in with role {Role}.", user.Username, user.Role);

        return RedirectToLocalOrHome(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name;

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        logger.LogInformation("User {Username} signed out.", username);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>
    /// Guards against an open redirect: a returnUrl arrives from the query string, so an
    /// attacker could otherwise use the login page to bounce a victim to another site.
    /// </summary>
    private IActionResult RedirectToLocalOrHome(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(HomeController.Index), "Home");
}
