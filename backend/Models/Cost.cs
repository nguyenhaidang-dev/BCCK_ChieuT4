namespace backend.Models;

public class Cost
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public Task? Task { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CostType Type { get; set; } // Fuel, Maintenance, etc.

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}