namespace backend.Models;

public class DriverLocation
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public User? Driver { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal? Speed { get; set; } // km/h
    public decimal? Heading { get; set; } // degrees
    public string? Status { get; set; } // Online, Offline, OnTask
}