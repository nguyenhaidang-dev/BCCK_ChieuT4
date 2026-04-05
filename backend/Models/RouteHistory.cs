namespace backend.Models;

public class RouteHistory
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public ApplicationUser? Driver { get; set; }
    public int TaskId { get; set; }
    public Task? Task { get; set; }
    public decimal StartLatitude { get; set; }
    public decimal StartLongitude { get; set; }
    public decimal EndLatitude { get; set; }
    public decimal EndLongitude { get; set; }
    public decimal DistanceKm { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal AverageSpeed { get; set; } // km/h
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string RoutePoints { get; set; } = string.Empty; // JSON array of lat/lng points
}