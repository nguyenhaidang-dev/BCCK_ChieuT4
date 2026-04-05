namespace backend.Models;

public class Trip
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public Task? Task { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal ActualDistanceKm { get; set; }
    public decimal ActualPrice { get; set; }
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}