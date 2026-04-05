using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public new DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<GoodsType> GoodsTypes { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Cost> Costs { get; set; }
    public DbSet<VehicleTypeFactor> VehicleTypeFactors { get; set; }
    public DbSet<DriverLocation> DriverLocations { get; set; }
    public DbSet<RouteHistory> RouteHistories { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<SOSEmergency> SOSEmergencies { get; set; }
    public DbSet<DriverExpense> DriverExpenses { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique index for Vehicle PlateNumber
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.PlateNumber)
            .IsUnique();

        // Unique index for User Email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Relationships
        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany() // User has many AuditLogs
            .HasForeignKey(a => a.UserId);

        // Configure Message relationships to avoid cascade cycles
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure other relationships
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Vehicle)
            .WithMany()
            .HasForeignKey(u => u.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure DriverLocation relationship
        modelBuilder.Entity<DriverLocation>()
            .HasOne(dl => dl.Driver)
            .WithMany()
            .HasForeignKey(dl => dl.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure decimal precision for VehicleTypeFactor.Factor
        modelBuilder.Entity<VehicleTypeFactor>()
            .Property(v => v.Factor)
            .HasPrecision(10, 4);

        // Configure decimal precision for SOSEmergency
        modelBuilder.Entity<SOSEmergency>()
            .Property(s => s.Longitude)
            .HasPrecision(10, 8);

        // Configure decimal precision for Task
        modelBuilder.Entity<Task>()
            .Property(t => t.DeliveryLatitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<Task>()
            .Property(t => t.DeliveryLongitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<Task>()
            .Property(t => t.DistanceKm)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Task>()
            .Property(t => t.EstimatedPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Task>()
            .Property(t => t.PickupLatitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<Task>()
            .Property(t => t.PickupLongitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<Task>()
            .Property(t => t.Weight)
            .HasPrecision(10, 2);

        // Configure decimal precision for Trip
        modelBuilder.Entity<Trip>()
            .Property(t => t.ActualDistanceKm)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Trip>()
            .Property(t => t.ActualPrice)
            .HasPrecision(18, 2);

        // Configure decimal precision for Vehicle
        modelBuilder.Entity<Vehicle>()
            .Property(v => v.MaxLoad)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Vehicle>()
            .Property(v => v.Volume)
            .HasPrecision(10, 2);

        // Configure decimal precision for DriverLocation
        modelBuilder.Entity<DriverLocation>()
            .Property(d => d.Latitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<DriverLocation>()
            .Property(d => d.Longitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<DriverLocation>()
            .Property(d => d.Speed)
            .HasPrecision(10, 2);

        modelBuilder.Entity<DriverLocation>()
            .Property(d => d.Heading)
            .HasPrecision(10, 2);

        // Configure decimal precision for SOSEmergency
        modelBuilder.Entity<SOSEmergency>()
            .Property(s => s.Latitude)
            .HasPrecision(10, 8);

        // Configure decimal precision for Cost
        modelBuilder.Entity<Cost>()
            .Property(c => c.Amount)
            .HasPrecision(18, 2);

        // Configure decimal precision for DriverExpense
        modelBuilder.Entity<DriverExpense>()
            .Property(d => d.Amount)
            .HasPrecision(18, 2);

        // Configure decimal precision for RouteHistory
        modelBuilder.Entity<RouteHistory>()
            .Property(r => r.StartLatitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<RouteHistory>()
            .Property(r => r.StartLongitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<RouteHistory>()
            .Property(r => r.EndLatitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<RouteHistory>()
            .Property(r => r.EndLongitude)
            .HasPrecision(10, 8);

        modelBuilder.Entity<RouteHistory>()
            .Property(r => r.DistanceKm)
            .HasPrecision(10, 2);

        modelBuilder.Entity<RouteHistory>()
            .Property(r => r.AverageSpeed)
            .HasPrecision(10, 2);

        // Configure relationships
        modelBuilder.Entity<Cost>()
            .HasOne(c => c.Task)
            .WithMany(t => t.Costs)
            .HasForeignKey(c => c.TaskId);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Task)
            .WithMany(t => t.Trips)
            .HasForeignKey(t => t.TaskId);

        modelBuilder.Entity<RouteHistory>()
            .HasOne(r => r.Driver)
            .WithMany()
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RouteHistory>()
            .HasOne(r => r.Task)
            .WithMany()
            .HasForeignKey(r => r.TaskId);

        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SOSEmergency>()
            .HasOne(s => s.Driver)
            .WithMany()
            .HasForeignKey(s => s.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DriverExpense>()
            .HasOne(d => d.Driver)
            .WithMany()
            .HasForeignKey(d => d.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Task relationships
        modelBuilder.Entity<Task>()
            .HasOne(t => t.AssignedDriver)
            .WithMany()
            .HasForeignKey(t => t.AssignedDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.CreatedByManager)
            .WithMany()
            .HasForeignKey(t => t.CreatedByManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global override: Ensure NO foreign key to ApplicationUser uses Cascade delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                if (foreignKey.PrincipalEntityType.ClrType == typeof(ApplicationUser) &&
                    foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }
}