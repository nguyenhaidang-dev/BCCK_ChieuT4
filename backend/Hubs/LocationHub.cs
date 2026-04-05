using Microsoft.AspNetCore.SignalR;
using backend.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using backend.Services;
using TaskModel = backend.Models.Task;

namespace backend.Hubs;

public class LocationHub : Hub
{
    private readonly IServiceProvider _serviceProvider;

    public LocationHub(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Method for drivers to send location updates
    public async System.Threading.Tasks.Task SendLocationUpdate(decimal latitude, decimal longitude, decimal? speed = null, decimal? heading = null)
    {
        var driverIdClaim = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (string.IsNullOrEmpty(driverIdClaim) || !int.TryParse(driverIdClaim, out var driverId))
        {
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pricingService = scope.ServiceProvider.GetRequiredService<PricingService>();

            // Save location to database
            var location = new DriverLocation
            {
                DriverId = driverId,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.Now,
                Speed = speed,
                Heading = heading,
                Status = "Online"
            };

            context.DriverLocations.Add(location);
            await context.SaveChangesAsync();

            // Check for geofencing alerts
            await CheckGeofencingAlerts(context, pricingService, driverId, latitude, longitude);

            // Broadcast to managers and dispatchers
            await Clients.Groups("Managers").SendAsync("ReceiveLocationUpdate", driverId.ToString(), latitude, longitude, speed, heading);
        }
    }

    private async System.Threading.Tasks.Task CheckGeofencingAlerts(AppDbContext context, PricingService pricingService, int driverId, decimal latitude, decimal longitude)
    {
        // Get active tasks for this driver
        var activeTasks = await context.Tasks
            .Where(t => t.AssignedDriverId == driverId &&
                        (t.Status == backend.Models.TaskStatus.Assigned || t.Status == backend.Models.TaskStatus.InProgress))
            .ToListAsync();

        foreach (var task in activeTasks)
        {
            decimal distanceToPickup = pricingService.CalculateDistance(latitude, longitude, task.PickupLatitude, task.PickupLongitude);
            decimal distanceToDelivery = pricingService.CalculateDistance(latitude, longitude, task.DeliveryLatitude, task.DeliveryLongitude);

            // Alert if within 500 meters of pickup location and task is assigned
            if (task.Status == backend.Models.TaskStatus.Assigned && distanceToPickup <= 0.5m)
            {
                await Clients.Caller.SendAsync("GeofencingAlert", new
                {
                    Type = "PickupNearby",
                    TaskId = task.Id,
                    ReferenceCode = task.ReferenceCode,
                    Distance = distanceToPickup,
                    Message = $"You are near the pickup location for task {task.ReferenceCode}"
                });
            }

            // Alert if within 500 meters of delivery location and task is in progress
            if (task.Status == backend.Models.TaskStatus.InProgress && distanceToDelivery <= 0.5m)
            {
                await Clients.Caller.SendAsync("GeofencingAlert", new
                {
                    Type = "DeliveryNearby",
                    TaskId = task.Id,
                    ReferenceCode = task.ReferenceCode,
                    Distance = distanceToDelivery,
                    Message = $"You are near the delivery location for task {task.ReferenceCode}"
                });
            }
        }
    }

    // Method for managers to request driver locations
    public async System.Threading.Tasks.Task RequestDriverLocations()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var locations = await context.DriverLocations
                .Where(l => l.Timestamp > DateTime.Now.AddMinutes(-5)) // Last 5 minutes
                .GroupBy(l => l.DriverId)
                .Select(g => g.OrderByDescending(l => l.Timestamp).First())
                .ToListAsync();

            await Clients.Caller.SendAsync("DriverLocationsUpdate", locations);
        }
    }

    public override async System.Threading.Tasks.Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.IsInRole("Manager") == true || user?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Managers");
        }

        await base.OnConnectedAsync();
    }

    public override async System.Threading.Tasks.Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}