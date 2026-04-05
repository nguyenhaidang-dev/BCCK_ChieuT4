using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Driver")]
public class DriverTasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PricingService _pricingService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public DriverTasksController(AppDbContext context, PricingService pricingService, UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _context = context;
        _pricingService = pricingService;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    // GET: api/DriverTasks
    [HttpGet]
    public async System.Threading.Tasks.Task<ActionResult<IEnumerable<backend.Models.Task>>> GetMyTasks([FromQuery] backend.Models.TaskStatus? status = null, [FromQuery] string? sortBy = null)
    {
        var driverId = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverId) || !int.TryParse(driverId, out var driverIdInt))
        {
            return Unauthorized();
        }

        var query = _context.Tasks
            .Where(t => t.AssignedDriverId == driverIdInt);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        switch (sortBy?.ToLower())
        {
            case "date":
                query = query.OrderBy(t => t.ScheduledPickupTime);
                break;
            case "priority":
                // Assuming higher estimated price means higher priority
                query = query.OrderByDescending(t => t.EstimatedPrice);
                break;
            default:
                query = query.OrderBy(t => t.CreatedAt);
                break;
        }

        var tasks = await query.ToListAsync();

        // Convert to DTO to ensure proper JSON serialization
        // Add tiny decimal offset to ensure values are never serialized as integers
        var taskDtos = tasks.Select(t => new TaskDto
        {
            Id = t.Id,
            ReferenceCode = t.ReferenceCode,
            PickupAddress = t.PickupAddress,
            PickupLatitude = (double)t.PickupLatitude + 0.0000001,
            PickupLongitude = (double)t.PickupLongitude + 0.0000001,
            DeliveryAddress = t.DeliveryAddress,
            DeliveryLatitude = (double)t.DeliveryLatitude + 0.0000001,
            DeliveryLongitude = (double)t.DeliveryLongitude + 0.0000001,
            DistanceKm = (double)t.DistanceKm + 0.0000001,
            Weight = (double)t.Weight + 0.0000001,
            VehicleType = t.VehicleType,
            EstimatedPrice = (double)t.EstimatedPrice + 0.0000001,
            Status = t.Status.ToString(),
            ScheduledPickupTime = t.ScheduledPickupTime,
            ActualPickupTime = t.ActualPickupTime,
            CompletedTime = t.CompletedTime,
            EstimatedArrivalTime = t.EstimatedArrivalTime,
            QrCode = t.QRCode
        }).ToList();

        return Ok(taskDtos);
    }

    // GET: api/DriverTasks/5
    [HttpGet("{id}")]
    public async System.Threading.Tasks.Task<ActionResult<backend.Models.Task>> GetTask(int id)
    {
        var driverId = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverId) || !int.TryParse(driverId, out var driverIdInt))
        {
            return Unauthorized();
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.AssignedDriverId == driverIdInt);

        if (task == null)
        {
            return NotFound();
        }

        // Convert to DTO to ensure proper JSON serialization
        // Add tiny decimal offset to ensure values are never serialized as integers
        var taskDto = new TaskDto
        {
            Id = task.Id,
            ReferenceCode = task.ReferenceCode,
            PickupAddress = task.PickupAddress,
            PickupLatitude = (double)task.PickupLatitude + 0.0000001,
            PickupLongitude = (double)task.PickupLongitude + 0.0000001,
            DeliveryAddress = task.DeliveryAddress,
            DeliveryLatitude = (double)task.DeliveryLatitude + 0.0000001,
            DeliveryLongitude = (double)task.DeliveryLongitude + 0.0000001,
            DistanceKm = (double)task.DistanceKm + 0.0000001,
            Weight = (double)task.Weight + 0.0000001,
            VehicleType = task.VehicleType,
            EstimatedPrice = (double)task.EstimatedPrice + 0.0000001,
            Status = task.Status.ToString(),
            ScheduledPickupTime = task.ScheduledPickupTime,
            ActualPickupTime = task.ActualPickupTime,
            CompletedTime = task.CompletedTime,
            EstimatedArrivalTime = task.EstimatedArrivalTime,
            QrCode = task.QRCode
        };

        return Ok(taskDto);
    }

    // PUT: api/DriverTasks/5/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var driverIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverIdStr) || !int.TryParse(driverIdStr, out var driverId))
        {
            return Unauthorized();
        }

        var task = await _context.Tasks.FindAsync(id);
        if (task == null || task.AssignedDriverId != driverId)
        {
            return NotFound();
        }

        if (!Enum.TryParse(request.Status, out backend.Models.TaskStatus parsedStatus))
        {
            return BadRequest(new { message = "Invalid status value" });
        }

        task.Status = parsedStatus;
        task.UpdatedAt = DateTime.Now;

        if (parsedStatus == backend.Models.TaskStatus.InProgress && !task.ActualPickupTime.HasValue)
        {
            task.ActualPickupTime = DateTime.Now;
        }
        else if (parsedStatus == backend.Models.TaskStatus.Completed && !task.CompletedTime.HasValue)
        {
            task.CompletedTime = DateTime.Now;
            // Generate electronic receipt
            task.ReceiptNumber = GenerateReceiptNumber();
            task.ReceiptGeneratedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/DriverTasks/start/5
    [HttpPost("start/{id}")]
    public async Task<IActionResult> StartTask(int id)
    {
        var driverIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverIdStr) || !int.TryParse(driverIdStr, out var driverId))
        {
            return Unauthorized();
        }

        var task = await _context.Tasks.FindAsync(id);
        if (task == null || task.AssignedDriverId != driverId)
        {
            return NotFound();
        }

        if (task.Status != backend.Models.TaskStatus.Assigned)
        {
            return BadRequest(new { message = "Task is not in Assigned status" });
        }

        task.Status = backend.Models.TaskStatus.InProgress;
        task.ActualPickupTime = DateTime.Now;
        task.UpdatedAt = DateTime.Now;

        // Calculate ETA from pickup to delivery location
        var eta = await _pricingService.CalculateETAAsync(task.PickupLatitude, task.PickupLongitude, task.DeliveryLatitude, task.DeliveryLongitude);
        task.EstimatedTravelTime = eta;
        task.EstimatedArrivalTime = task.ActualPickupTime.Value.Add(eta);

        await _context.SaveChangesAsync();

        // Convert to DTO to ensure proper JSON serialization
        // Add tiny decimal offset to ensure values are never serialized as integers
        var taskDto = new TaskDto
        {
            Id = task.Id,
            ReferenceCode = task.ReferenceCode,
            PickupAddress = task.PickupAddress,
            PickupLatitude = (double)task.PickupLatitude + 0.0000001,
            PickupLongitude = (double)task.PickupLongitude + 0.0000001,
            DeliveryAddress = task.DeliveryAddress,
            DeliveryLatitude = (double)task.DeliveryLatitude + 0.0000001,
            DeliveryLongitude = (double)task.DeliveryLongitude + 0.0000001,
            DistanceKm = (double)task.DistanceKm + 0.0000001,
            Weight = (double)task.Weight + 0.0000001,
            VehicleType = task.VehicleType,
            EstimatedPrice = (double)task.EstimatedPrice + 0.0000001,
            Status = task.Status.ToString(),
            ScheduledPickupTime = task.ScheduledPickupTime,
            ActualPickupTime = task.ActualPickupTime,
            CompletedTime = task.CompletedTime,
            EstimatedArrivalTime = task.EstimatedArrivalTime,
            QrCode = task.QRCode
        };

        return Ok(taskDto);
    }

    // POST: api/DriverTasks/pickup/5
    [HttpPost("pickup/{id}")]
    public async Task<IActionResult> ConfirmPickup(int id)
    {
        var driverIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverIdStr) || !int.TryParse(driverIdStr, out var driverId))
        {
            return Unauthorized();
        }

        var task = await _context.Tasks.FindAsync(id);
        if (task == null || task.AssignedDriverId != driverId)
        {
            return NotFound();
        }

        if (task.Status != backend.Models.TaskStatus.InProgress)
        {
            return BadRequest(new { message = "Task is not in InProgress status" });
        }

        task.Status = backend.Models.TaskStatus.PickedUp;
        task.ActualPickupTime = DateTime.Now;  // Update pickup time
        task.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // Convert to DTO to ensure proper JSON serialization
        // Add tiny decimal offset to ensure values are never serialized as integers
        var taskDto = new TaskDto
        {
            Id = task.Id,
            ReferenceCode = task.ReferenceCode,
            PickupAddress = task.PickupAddress,
            PickupLatitude = (double)task.PickupLatitude + 0.0000001,
            PickupLongitude = (double)task.PickupLongitude + 0.0000001,
            DeliveryAddress = task.DeliveryAddress,
            DeliveryLatitude = (double)task.DeliveryLatitude + 0.0000001,
            DeliveryLongitude = (double)task.DeliveryLongitude + 0.0000001,
            DistanceKm = (double)task.DistanceKm + 0.0000001,
            Weight = (double)task.Weight + 0.0000001,
            VehicleType = task.VehicleType,
            EstimatedPrice = (double)task.EstimatedPrice + 0.0000001,
            Status = task.Status.ToString(),
            ScheduledPickupTime = task.ScheduledPickupTime,
            ActualPickupTime = task.ActualPickupTime,
            CompletedTime = task.CompletedTime,
            EstimatedArrivalTime = task.EstimatedArrivalTime,
            QrCode = task.QRCode
        };

        return Ok(taskDto);
    }

    // POST: api/DriverTasks/complete/5
    [HttpPost("complete/{id}")]
    public async Task<IActionResult> CompleteTask(int id)
    {
        var driverIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverIdStr) || !int.TryParse(driverIdStr, out var driverId))
        {
            return Unauthorized();
        }

        var task = await _context.Tasks.FindAsync(id);
        if (task == null || task.AssignedDriverId != driverId)
        {
            return NotFound();
        }

        if (task.Status != backend.Models.TaskStatus.PickedUp)
        {
            return BadRequest(new { message = "Task is not in PickedUp status" });
        }

        task.Status = backend.Models.TaskStatus.Completed;
        task.CompletedTime = DateTime.Now;
        task.UpdatedAt = DateTime.Now;
        // Generate electronic receipt
        task.ReceiptNumber = GenerateReceiptNumber();
        task.ReceiptGeneratedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // Convert to DTO to ensure proper JSON serialization
        // Add tiny decimal offset to ensure values are never serialized as integers
        var taskDto = new TaskDto
        {
            Id = task.Id,
            ReferenceCode = task.ReferenceCode,
            PickupAddress = task.PickupAddress,
            PickupLatitude = (double)task.PickupLatitude + 0.0000001,
            PickupLongitude = (double)task.PickupLongitude + 0.0000001,
            DeliveryAddress = task.DeliveryAddress,
            DeliveryLatitude = (double)task.DeliveryLatitude + 0.0000001,
            DeliveryLongitude = (double)task.DeliveryLongitude + 0.0000001,
            DistanceKm = (double)task.DistanceKm + 0.0000001,
            Weight = (double)task.Weight + 0.0000001,
            VehicleType = task.VehicleType,
            EstimatedPrice = (double)task.EstimatedPrice + 0.0000001,
            Status = task.Status.ToString(),
            ScheduledPickupTime = task.ScheduledPickupTime,
            ActualPickupTime = task.ActualPickupTime,
            CompletedTime = task.CompletedTime,
            EstimatedArrivalTime = task.EstimatedArrivalTime,
            QrCode = task.QRCode
        };

        return Ok(taskDto);
    }

    // GET: api/DriverTasks/history
    [HttpGet("history")]
    public async System.Threading.Tasks.Task<ActionResult<IEnumerable<backend.Models.Task>>> GetTaskHistory([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var driverIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverIdStr) || !int.TryParse(driverIdStr, out var driverId))
        {
            return Unauthorized();
        }

        var query = _context.Tasks
            .Where(t => t.AssignedDriverId == driverId && t.Status == backend.Models.TaskStatus.Completed);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CompletedTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CompletedTime <= endDate.Value);
        }

        var tasks = await query.OrderByDescending(t => t.CompletedTime).ToListAsync();

        // Convert to DTO to ensure proper JSON serialization
        // Add tiny decimal offset to ensure values are never serialized as integers
        var taskDtos = tasks.Select(t => new TaskDto
        {
            Id = t.Id,
            ReferenceCode = t.ReferenceCode,
            PickupAddress = t.PickupAddress,
            PickupLatitude = (double)t.PickupLatitude + 0.0000001,
            PickupLongitude = (double)t.PickupLongitude + 0.0000001,
            DeliveryAddress = t.DeliveryAddress,
            DeliveryLatitude = (double)t.DeliveryLatitude + 0.0000001,
            DeliveryLongitude = (double)t.DeliveryLongitude + 0.0000001,
            DistanceKm = (double)t.DistanceKm + 0.0000001,
            Weight = (double)t.Weight + 0.0000001,
            VehicleType = t.VehicleType,
            EstimatedPrice = (double)t.EstimatedPrice + 0.0000001,
            Status = t.Status.ToString(),
            ScheduledPickupTime = t.ScheduledPickupTime,
            ActualPickupTime = t.ActualPickupTime,
            CompletedTime = t.CompletedTime,
            EstimatedArrivalTime = t.EstimatedArrivalTime,
            QrCode = t.QRCode
        }).ToList();

        return Ok(taskDtos);
    }

    // GET: api/DriverTasks/earnings
    [HttpGet("earnings")]
    public async Task<ActionResult<EarningsSummary>> GetEarnings([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var driverIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverIdStr) || !int.TryParse(driverIdStr, out var driverId))
        {
            return Unauthorized();
        }

        var query = _context.Tasks
            .Where(t => t.AssignedDriverId == driverId && t.Status == backend.Models.TaskStatus.Completed);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CompletedTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CompletedTime <= endDate.Value);
        }

        var completedTasks = await query.ToListAsync();

        var totalEarnings = completedTasks.Sum(t => t.EstimatedPrice);
        var taskCount = completedTasks.Count;

        // Calculate tips and bonuses if any (assuming stored in Costs or separate field)
        // For now, just return basic earnings

        var summary = new EarningsSummary
        {
            TotalEarnings = totalEarnings,
            TaskCount = taskCount,
            Period = startDate.HasValue && endDate.HasValue ? $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}" : "All time"
        };

        return Ok(summary);
    }

    // POST: api/DriverTasks/verify-qr
    [HttpPost("verify-qr")]
    public async Task<IActionResult> VerifyQRCode([FromBody] QRVerificationRequest request)
    {
        var driverIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverIdStr) || !int.TryParse(driverIdStr, out var driverId))
        {
            return Unauthorized();
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.QRCode == request.QRCode && t.AssignedDriverId == driverId);

        if (task == null)
        {
            return BadRequest(new { message = "Invalid QR code or task not assigned to you" });
        }

        // Update task status based on verification type (pickup or delivery)
        if (request.Type == "pickup" && task.Status == backend.Models.TaskStatus.Assigned)
        {
            task.Status = backend.Models.TaskStatus.InProgress;
            task.ActualPickupTime = DateTime.Now;
        }
        else if (request.Type == "delivery" && task.Status == backend.Models.TaskStatus.InProgress)
        {
            task.Status = backend.Models.TaskStatus.Completed;
            task.CompletedTime = DateTime.Now;
        }

        task.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { message = "QR code verified successfully", taskId = task.Id, status = task.Status });
    }

    // GET: api/DriverTasks/route-history
    [HttpGet("route-history")]
    public async Task<ActionResult<IEnumerable<RouteHistory>>> GetRouteHistory([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var driverId = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverId))
        {
            return Unauthorized();
        }

        var userEmail = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }

        var query = _context.RouteHistories
            .Include(r => r.Task)
            .Where(r => r.DriverId == user.Id);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.EndTime <= endDate.Value);
        }

        var routes = await query.OrderByDescending(r => r.StartTime).ToListAsync();
        return Ok(routes);
    }

    // GET: api/DriverTasks/performance-analytics
    [HttpGet("performance-analytics")]
    public async Task<ActionResult<PerformanceAnalytics>> GetPerformanceAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var driverId = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverId))
        {
            return Unauthorized();
        }

        var userEmail = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }

        var query = _context.RouteHistories
            .Where(r => r.DriverId == user.Id);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.EndTime <= endDate.Value);
        }

        var routes = await query.ToListAsync();

        if (!routes.Any())
        {
            return Ok(new PerformanceAnalytics
            {
                TotalRoutes = 0,
                TotalDistance = 0,
                TotalDuration = TimeSpan.Zero,
                AverageSpeed = 0,
                BestSpeed = 0,
                Period = startDate.HasValue && endDate.HasValue ? $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}" : "All time"
            });
        }

        var analytics = new PerformanceAnalytics
        {
            TotalRoutes = routes.Count,
            TotalDistance = routes.Sum(r => r.DistanceKm),
            TotalDuration = TimeSpan.FromTicks(routes.Sum(r => r.Duration.Ticks)),
            AverageSpeed = routes.Average(r => r.AverageSpeed),
            BestSpeed = routes.Max(r => r.AverageSpeed),
            Period = startDate.HasValue && endDate.HasValue ? $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}" : "All time"
        };

        return Ok(analytics);
    }

    // POST: api/DriverTasks/sos
    [HttpPost("sos")]
    public async Task<IActionResult> SendSOS([FromBody] SOSRequest request)
    {
        var driverId = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverId))
        {
            return Unauthorized();
        }

        var userEmail = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }

        // Get driver's current location
        if (!int.TryParse(driverId, out var driverIdInt))
        {
            return BadRequest("Invalid driver ID");
        }

        var lastLocation = await _context.DriverLocations
            .Where(l => l.DriverId == driverIdInt)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefaultAsync();

        // Create emergency alert
        var sosAlert = new SOSEmergency
        {
            DriverId = user.Id,
            Latitude = lastLocation?.Latitude ?? 0,
            Longitude = lastLocation?.Longitude ?? 0,
            EmergencyType = request.EmergencyType,
            Description = request.Description,
            ReportedAt = DateTime.Now,
            Status = EmergencyStatus.Active
        };

        _context.SOSEmergencies.Add(sosAlert);
        await _context.SaveChangesAsync();

        // Send notification to managers
        await _notificationService.SendLocationAlert(driverId, $"SOS Emergency: {request.EmergencyType} - {request.Description}");

        // In a real implementation, you would also:
        // - Send SMS to emergency contacts
        // - Call emergency services
        // - Send push notifications to managers

        return Ok(new { message = "SOS alert sent successfully", alertId = sosAlert.Id });
    }

    // GET: api/DriverTasks/expenses
    [HttpGet("expenses")]
    public async Task<ActionResult<IEnumerable<DriverExpense>>> GetMyExpenses([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var driverId = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverId))
        {
            return Unauthorized();
        }

        var userEmail = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }

        var query = _context.DriverExpenses
            .Where(e => e.DriverId == user.Id);

        if (startDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= endDate.Value);
        }

        var expenses = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();
        return Ok(expenses);
    }

    // POST: api/DriverTasks/expenses
    [HttpPost("expenses")]
    public async Task<ActionResult<DriverExpense>> AddExpense([FromBody] AddExpenseRequest request)
    {
        var driverId = GetCurrentUserId();
        if (string.IsNullOrEmpty(driverId))
        {
            return Unauthorized();
        }

        var userEmail = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }

        var expense = new DriverExpense
        {
            DriverId = user.Id,
            Type = request.Type,
            Amount = request.Amount,
            Description = request.Description,
            ExpenseDate = request.ExpenseDate,
            Status = ExpenseStatus.Pending,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.DriverExpenses.Add(expense);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyExpenses), new { id = expense.Id }, expense);
    }

    private string GenerateReceiptNumber()
    {
        return $"RCP-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}

public class UpdateTaskStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class EarningsSummary
{
    public decimal TotalEarnings { get; set; }
    public int TaskCount { get; set; }
    public string Period { get; set; } = string.Empty;
}

public class QRVerificationRequest
{
    public string QRCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "pickup" or "delivery"
}

public class PerformanceAnalytics
{
    public int TotalRoutes { get; set; }
    public decimal TotalDistance { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public decimal AverageSpeed { get; set; }
    public decimal BestSpeed { get; set; }
    public string Period { get; set; } = string.Empty;
}

public class SOSRequest
{
    public string EmergencyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AddExpenseRequest
{
    public ExpenseType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
}

public class TaskDto
{
    public int Id { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }
    public double DistanceKm { get; set; }
    public double Weight { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public double EstimatedPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledPickupTime { get; set; }
    public DateTime? ActualPickupTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public DateTime? EstimatedArrivalTime { get; set; }
    public string QrCode { get; set; } = string.Empty;
}
// Improved query performance.
