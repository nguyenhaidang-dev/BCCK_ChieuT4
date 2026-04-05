namespace backend.Models;

public class VehicleTypeFactor
{
    public int Id { get; set; }
    public string VehicleType { get; set; } = string.Empty; // e.g., "1 Tấn", "5 Tấn", "Container 40ft"
    public decimal Factor { get; set; } // Multiplier for pricing, e.g., 1.0 for standard, 1.5 for larger vehicles

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}