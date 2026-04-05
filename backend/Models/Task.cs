using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class Task
{
    public int Id { get; set; }

    public string ReferenceCode { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;

    public string PickupAddress { get; set; } = string.Empty;

    [Column(TypeName = "decimal(11,8)")]
    public decimal PickupLatitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal PickupLongitude { get; set; }

    public string DeliveryAddress { get; set; } = string.Empty;

    [Column(TypeName = "decimal(11,8)")]
    public decimal DeliveryLatitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal DeliveryLongitude { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DistanceKm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Weight { get; set; }

    public string VehicleType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedPrice { get; set; }

    public TaskStatus Status { get; set; }

    public DateTime ScheduledPickupTime { get; set; }
    public DateTime? ActualPickupTime { get; set; }
    public DateTime? CompletedTime { get; set; }

    public TimeSpan? EstimatedTravelTime { get; set; }
    public DateTime? EstimatedArrivalTime { get; set; }

    public string? ReceiptNumber { get; set; }
    public DateTime? ReceiptGeneratedAt { get; set; }

    // Foreign keys
    public int? AssignedDriverId { get; set; }
    public int CreatedByManagerId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser? AssignedDriver { get; set; }
    public ApplicationUser? CreatedByManager { get; set; }
    public ICollection<Trip>? Trips { get; set; }
    public ICollection<Cost>? Costs { get; set; }
}
