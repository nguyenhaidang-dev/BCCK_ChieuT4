using backend.Models;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using TaskModel = backend.Models.Task;

namespace backend.Services;

public class NotificationService
{
    private readonly IHubContext<LocationHub> _hubContext;

    public NotificationService(IHubContext<LocationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async System.Threading.Tasks.Task SendTaskAssignedNotification(string driverId, TaskModel task)
    {
        var notification = new
        {
            Type = "TaskAssigned",
            TaskId = task.Id,
            ReferenceCode = task.ReferenceCode,
            PickupAddress = task.PickupAddress,
            DeliveryAddress = task.DeliveryAddress,
            ScheduledPickupTime = task.ScheduledPickupTime,
            EstimatedPrice = task.EstimatedPrice,
            Message = $"New task assigned: {task.ReferenceCode}"
        };

        // Send to specific driver via SignalR
        await _hubContext.Clients.User(driverId).SendAsync("ReceiveNotification", notification);

        // In a real implementation, you would also send push notifications to mobile devices
        // This would require integrating with FCM, APNs, or similar services
    }

    public async System.Threading.Tasks.Task SendTaskUpdateNotification(string driverId, TaskModel task)
    {
        var notification = new
        {
            Type = "TaskUpdate",
            TaskId = task.Id,
            Status = task.Status,
            Message = $"Task {task.ReferenceCode} status updated to {task.Status}"
        };

        await _hubContext.Clients.User(driverId).SendAsync("ReceiveNotification", notification);
    }

    public async System.Threading.Tasks.Task SendLocationAlert(string driverId, string message)
    {
        var notification = new
        {
            Type = "LocationAlert",
            Message = message
        };

        await _hubContext.Clients.User(driverId).SendAsync("ReceiveNotification", notification);
    }
}