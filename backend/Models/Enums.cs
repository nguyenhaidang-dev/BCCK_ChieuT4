namespace backend.Models;

public enum Role
{
    Driver,
    Manager,
    Admin
}

public enum UserStatus
{
    Active,
    Inactive
}

public enum VehicleStatus
{
    Available,
    InMaintenance
}

public enum TaskStatus
{
    Unassigned,    // Chưa phân công
    Assigned,      // Đã phân công
    InProgress,    // Đang thực hiện
    PickedUp,      // Đã lấy hàng
    Completed,     // Hoàn thành
    Cancelled      // Đã hủy
}

public enum CostType
{
    Fuel,
    Maintenance,
    Toll,
    Parking,
    Other
}