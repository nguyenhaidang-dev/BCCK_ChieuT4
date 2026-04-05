namespace backend.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal MaxLoad { get; set; }
    public decimal Volume { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public VehicleStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}