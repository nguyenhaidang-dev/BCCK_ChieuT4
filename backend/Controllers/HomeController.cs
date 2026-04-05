using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);

        ViewBag.UserRoles = roles;
        ViewBag.IsAdmin = roles.Contains("Admin");
        ViewBag.IsManager = roles.Contains("Manager");

        var totalDrivers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active);
        var activeDrivers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active);
        var totalVehicles = await _context.Vehicles.CountAsync();
        var activeVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Available);
        var totalTasks = await _context.Tasks.CountAsync();
        var pendingTasks = await _context.Tasks.CountAsync(t => t.Status == backend.Models.TaskStatus.Unassigned || t.Status == backend.Models.TaskStatus.Assigned);

        ViewBag.TotalDrivers = totalDrivers;
        ViewBag.ActiveDrivers = activeDrivers;
        ViewBag.TotalVehicles = totalVehicles;
        ViewBag.ActiveVehicles = activeVehicles;
        ViewBag.TotalTasks = totalTasks;
        ViewBag.PendingTasks = pendingTasks;

        return View();
    }
}