using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class UserCreateModel
{
    [Required(ErrorMessage = "Tên là bắt buộc")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vai trò là bắt buộc")]
    public Role Role { get; set; }

    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public UserStatus Status { get; set; }

    // For Drivers
    public string? LicenseNumber { get; set; }
    public string? LicenseType { get; set; }

    // For Managers/Admins
    public string? AccessRights { get; set; }
}