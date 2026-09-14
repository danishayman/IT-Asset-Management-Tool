using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

/// <summary>
/// Placeholder so the authentication milestone is independently verifiable. The asset
/// list, search and CRUD actions land in the next milestone.
/// </summary>
[Authorize]
public class AssetsController : Controller
{
    public IActionResult Index() => View();
}
