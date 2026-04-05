using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Controllers;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: UserManagement
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var userRolesViewModel = new List<UserRolesViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRolesViewModel.Add(new UserRolesViewModel
            {
                UserId = user.Id.ToString(),
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Name = user.Name,
                Status = user.Status,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt
            });
        }

        return View(userRolesViewModel);
    }

    // GET: UserManagement/Create
    public IActionResult Create()
    {
        ViewBag.Roles = _roleManager.Roles.ToList();
        return View();
    }

    // POST: UserManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                Status = model.Status,
                EmailConfirmed = true
            };

            // Add driver-specific fields if role is Driver
            if (model.SelectedRole == "Driver")
            {
                user.LicenseNumber = model.LicenseNumber;
                user.LicenseType = model.LicenseType;
            }

            // Add manager-specific fields if role is Manager
            if (model.SelectedRole == "Manager")
            {
                user.AccessRights = model.AccessRights;
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Roles = _roleManager.Roles.ToList();
        return View(model);
    }

    // GET: UserManagement/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var model = new EditUserViewModel
        {
            Id = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Status = user.Status,
            LicenseNumber = user.LicenseNumber,
            LicenseType = user.LicenseType,
            AccessRights = user.AccessRights,
            SelectedRole = userRoles.FirstOrDefault() ?? string.Empty
        };

        ViewBag.Roles = _roleManager.Roles.ToList();
        return View(model);
    }

    // POST: UserManagement/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.Name = model.Name;
            user.Status = model.Status;
            user.LicenseNumber = model.LicenseNumber;
            user.LicenseType = model.LicenseType;
            user.AccessRights = model.AccessRights;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update role
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.SelectedRole);

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Roles = _roleManager.Roles.ToList();
        return View(model);
    }

    // POST: UserManagement/ResetPassword/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            TempData["Message"] = "Mật khẩu đã được đặt lại thành công";
        }
        else
        {
            TempData["Error"] = "Có lỗi xảy ra khi đặt lại mật khẩu";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: UserManagement/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Status = user.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Index));
    }
}

public class UserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public DateTime? CreatedAt { get; set; }
}

public class CreateUserViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string SelectedRole { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;

    // Driver fields
    public string? LicenseNumber { get; set; }
    public string? LicenseType { get; set; }

    // Manager fields
    public string? AccessRights { get; set; }
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public string SelectedRole { get; set; } = string.Empty;

    // Driver fields
    public string? LicenseNumber { get; set; }
    public string? LicenseType { get; set; }

    // Manager fields
    public string? AccessRights { get; set; }
}