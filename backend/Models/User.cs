namespace backend.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // For Drivers
    public string? LicenseNumber { get; set; }
    public string? LicenseType { get; set; }

    // For Managers/Admins
    public string? AccessRights { get; set; }

    // Navigation properties
    public ICollection<DriverLocation>? DriverLocations { get; set; }
    public ICollection<Task>? AssignedTasks { get; set; }
}