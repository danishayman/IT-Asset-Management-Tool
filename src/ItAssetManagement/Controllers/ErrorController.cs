using ItAssetManagement.Infrastructure;
using ItAssetManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

/// <summary>
/// Friendly pages for the failure paths. Anonymous throughout: something going wrong
/// while signed out must not itself redirect to a login page.
/// </summary>
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ErrorController(ILogger<ErrorController> logger) : Controller
{
    /// <summary>Handles an unhandled exception, reached by re-execution from UseExceptionHandler.</summary>
    [Route("/Error")]
    public IActionResult Index()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var correlationId = HttpContext.GetCorrelationId();

        // Logged here rather than relying on the framework, so the entry carries the
        // correlation id and the path that actually failed.
        logger.LogError(
            feature?.Error,
            "Unhandled exception at {Path}. Correlation id {CorrelationId}.",
            feature?.Path ?? "unknown",
            correlationId);

        Response.StatusCode = StatusCodes.Status500InternalServerError;

        return View(new ErrorViewModel { CorrelationId = correlationId });
    }

    /// <summary>
    /// Handles status codes that never threw, reached by re-execution from
    /// UseStatusCodePagesWithReExecute: a 404 for an unknown route, or a 403 from
    /// authorisation.
    /// </summary>
    [Route("/Error/{statusCode:int}")]
    public IActionResult Status(int statusCode)
    {
        var model = new ErrorViewModel { CorrelationId = HttpContext.GetCorrelationId() };

        return statusCode switch
        {
            StatusCodes.Status403Forbidden => View("AccessDenied", model),
            StatusCodes.Status404NotFound => View("NotFound", model),
            _ => View("Index", model)
        };
    }

    /// <summary>
    /// Deliberately throws, so the global handler and the correlation id can actually be
    /// exercised. Hidden in Production: a real deployment has no business exposing a route
    /// whose only purpose is to fail.
    /// </summary>
    [Route("/Error/Throw")]
    public IActionResult Throw([FromServices] IWebHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            return NotFound();
        }

        throw new InvalidOperationException("Deliberate exception for verifying the global error handler.");
    }
}
