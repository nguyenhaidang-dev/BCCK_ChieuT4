using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Services;
using System.Threading.Tasks;

namespace backend.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class TasksController : Controller
{
    private readonly AppDbContext _context;
    private readonly PricingService _pricingService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public TasksController(AppDbContext context, PricingService pricingService, UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _context = context;
        _pricingService = pricingService;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    // GET: Tasks
    public async Task<IActionResult> Index()
    {
        var tasks = await _context.Tasks.ToListAsync();

        var driverIds = tasks
            .Where(t => t.AssignedDriverId.HasValue)
            .Select(t => (int)t.AssignedDriverId!)
            .Distinct()
            .ToList();

        var drivers = await _context.Users
            .Where(u => driverIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var taskWithDrivers = tasks.Select(t => new TaskWithDriver
        {
            Task = t,
            DriverName = t.AssignedDriverId.HasValue && drivers.TryGetValue(t.AssignedDriverId.Value, out var name) ? name : "Chưa phân công"
        }).ToList();

        return View(taskWithDrivers);
    }

    // GET: Tasks/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var task = await _context.Tasks
            .Include(t => t.Trips)
            .Include(t => t.Costs)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (task == null)
        {
            return NotFound();
        }

        return View(task);
    }

    // GET: Tasks/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Tasks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTaskViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Calculate distance using OSRM API
            var distance = await _pricingService.CalculateDistanceAsync(model.PickupLatitude, model.PickupLongitude,
                                                                        model.DeliveryLatitude, model.DeliveryLongitude);

            // Calculate ETA
            var eta = await _pricingService.CalculateETAAsync(model.PickupLatitude, model.PickupLongitude,
                                                             model.DeliveryLatitude, model.DeliveryLongitude);

            // Calculate estimated price
            var estimatedPrice = await _pricingService.CalculateEstimatedPrice(distance, model.VehicleType, model.Weight);

            // Generate reference code
            var referenceCode = GenerateReferenceCode();

            // Generate QR code data
            var qrCode = GenerateQRCode(referenceCode);

            var task = new backend.Models.Task
            {
                ReferenceCode = referenceCode,
                QRCode = qrCode,
                PickupAddress = model.PickupAddress,
                PickupLatitude = model.PickupLatitude,
                PickupLongitude = model.PickupLongitude,
                DeliveryAddress = model.DeliveryAddress,
                DeliveryLatitude = model.DeliveryLatitude,
                DeliveryLongitude = model.DeliveryLongitude,
                DistanceKm = distance,
                Weight = model.Weight,
                VehicleType = model.VehicleType,
                EstimatedPrice = estimatedPrice,
                EstimatedTravelTime = eta,
                EstimatedArrivalTime = model.ScheduledPickupTime.Add(eta),
                Status = backend.Models.TaskStatus.Unassigned,
                ScheduledPickupTime = model.ScheduledPickupTime,
                CreatedByManagerId = int.Parse(GetCurrentUserId()),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Add(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Tasks/Tracking
    public async Task<IActionResult> Tracking()
    {
        // Get all active drivers from custom User table
        var drivers = await _context.Users
            .Where(u => u.Role == Role.Driver && u.Status == UserStatus.Active)
            .ToListAsync();

        // Get latest locations for these drivers
        var driverIds = drivers.Select(d => d.Id).ToList();
        var locations = await _context.DriverLocations
            .Where(dl => driverIds.Contains(dl.DriverId))
            .GroupBy(dl => dl.DriverId)
            .Select(g => g.OrderByDescending(dl => dl.Timestamp).First())
            .ToListAsync();

        // Get current tasks
        var tasks = await _context.Tasks
            .Where(t => t.AssignedDriverId.HasValue && driverIds.Contains(t.AssignedDriverId.Value) &&
                        (t.Status == backend.Models.TaskStatus.InProgress || t.Status == backend.Models.TaskStatus.Assigned))
            .ToListAsync();

        // Combine data
        var driverData = drivers.Select(driver => new DriverTrackingViewModel
        {
            Driver = driver,
            LatestLocation = locations.FirstOrDefault(l => l.DriverId == driver.Id),
            CurrentTask = tasks.FirstOrDefault(t => t.AssignedDriverId == driver.Id)
        }).ToList();

        return View(driverData);
    }

    public class DriverTrackingViewModel
    {
        public User Driver { get; set; } = null!;
        public DriverLocation? LatestLocation { get; set; }
        public backend.Models.Task? CurrentTask { get; set; }
    }

    // GET: Tasks/Assign/5
    public async Task<IActionResult> Assign(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        var availableDrivers = await _context.Users
            .Where(u => u.Role == Role.Driver && u.Status == UserStatus.Active)
            .ToListAsync();

        ViewBag.AvailableDrivers = availableDrivers;
        return View(task);
    }

    // POST: Tasks/Assign/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, string driverId)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        if (!int.TryParse(driverId, out var driverIdInt))
        {
            ModelState.AddModelError("", "ID tài xế không hợp lệ");
            // Reload available drivers
            var availableDrivers = await _context.Users
                .Where(u => u.Role == Role.Driver && u.Status == UserStatus.Active)
                .ToListAsync();
            ViewBag.AvailableDrivers = availableDrivers;
            return View(task);
        }

        var driver = await _context.Users.FindAsync(driverIdInt);
        if (driver == null || driver.Status != UserStatus.Active)
        {
            ModelState.AddModelError("", "Tài xế không hợp lệ hoặc không hoạt động");
            // Reload available drivers
            var availableDrivers = await _context.Users
                .Where(u => u.Role == Role.Driver && u.Status == UserStatus.Active)
                .ToListAsync();
            ViewBag.AvailableDrivers = availableDrivers;
            return View(task);
        }

        task.AssignedDriverId = driverIdInt;
        task.Status = backend.Models.TaskStatus.Assigned;
        task.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // Send notification to driver
        await _notificationService.SendTaskAssignedNotification(driverId, task);

        return RedirectToAction(nameof(Index));
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }


    private string GenerateReferenceCode()
    {
        return $"TASK-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    // GET: api/tasks/driver-location/{driverId}
    [HttpGet("driver-location/{driverId}")]
    public async Task<IActionResult> GetDriverLocation(string driverId)
    {
        if (!int.TryParse(driverId, out var driverIdInt))
        {
            return BadRequest("Invalid driver ID");
        }

        var location = await _context.DriverLocations
            .Where(dl => dl.DriverId == driverIdInt)
            .OrderByDescending(dl => dl.Timestamp)
            .FirstOrDefaultAsync();

        if (location == null)
        {
            return NotFound();
        }

        return Json(new
        {
            latitude = location.Latitude,
            longitude = location.Longitude,
            timestamp = location.Timestamp
        });
    }

    private string GenerateQRCode(string referenceCode)
    {
        // For simplicity, use the reference code as QR data
        // In a real implementation, you might use a library like QRCoder to generate actual QR codes
        return referenceCode;
    }
}

public class TaskWithDriver
{
    public backend.Models.Task Task { get; set; } = null!;
    public string DriverName { get; set; } = "Chưa phân công";
}

public class CreateTaskViewModel
{
    public string PickupAddress { get; set; } = string.Empty;
    public decimal PickupLatitude { get; set; }
    public decimal PickupLongitude { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal DeliveryLatitude { get; set; }
    public decimal DeliveryLongitude { get; set; }
    public decimal Weight { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public DateTime ScheduledPickupTime { get; set; }
}