using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize]
public class AdminController : Controller
{
    public IActionResult Dashboard()
    {
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home");
    }
}