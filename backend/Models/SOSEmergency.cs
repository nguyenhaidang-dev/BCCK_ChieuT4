namespace backend.Models;

public class SOSEmergency
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public ApplicationUser? Driver { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string EmergencyType { get; set; } = string.Empty; // "Accident", "Breakdown", "Medical", "Other"
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public EmergencyStatus Status { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum EmergencyStatus
{
    Active,
    Resolved,
    FalseAlarm
}