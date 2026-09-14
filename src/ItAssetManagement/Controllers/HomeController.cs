using Microsoft.AspNetCore.Mvc;

namespace ItAssetManagement.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
