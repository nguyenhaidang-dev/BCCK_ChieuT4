using Microsoft.AspNetCore.Identity;

namespace backend.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? Address { get; set; }

    // For Drivers
    public string? LicenseNumber { get; set; }
    public string? LicenseType { get; set; }
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    // For Managers/Admins
    public string? AccessRights { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties for our custom entities can be added later if needed
}