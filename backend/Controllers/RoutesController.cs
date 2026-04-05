using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string _osrmBaseUrl;

    public RoutesController(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _osrmBaseUrl = configuration.GetValue<string>("RoutingService:OsrmBaseUrl") ?? "http://router.project-osrm.org";
    }

    // GET: api/routes/get-task-route?taskId=1
    [HttpGet("get-task-route")]
    public async Task<IActionResult> GetTaskRoute([FromQuery] string taskId, [FromServices] AppDbContext context)
    {
        if (!int.TryParse(taskId, out var taskIdInt))
        {
            return BadRequest("Invalid task ID");
        }

        var task = await context.Tasks.FindAsync(taskIdInt);
        if (task == null)
        {
            return NotFound("Task not found");
        }

        // Get directions from current location (assuming driver's location) to pickup/delivery
        // For demo, use fixed start location
        var startLat = 10.8231; // Ho Chi Minh City center
        var startLng = 106.6297;

        var endLat = task.PickupLatitude;
        var endLng = task.PickupLongitude;

        // For now, return sample data - in production, call GetDirections
        var sampleRoute = new
        {
            coordinates = new[]
            {
                new { lat = startLat, lng = startLng },
                new { lat = (startLat + (double)endLat) / 2, lng = (startLng + (double)endLng) / 2 },
                new { lat = (double)endLat, lng = (double)endLng }
            },
            steps = new[]
            {
                new { instruction = "Đi thẳng về phía Bắc trên đường Nguyễn Huệ", distance = 500, duration = 120 },
                new { instruction = "Rẽ phải vào đường Lê Lợi", distance = 300, duration = 90 },
                new { instruction = "Tiếp tục đi thẳng đến điểm đến", distance = 200, duration = 60 }
            }
        };

        return Ok(sampleRoute);
    }

    // GET: api/routes/directions?startLat=10.8231&startLng=106.6297&endLat=10.8800&endLng=106.6800
    [HttpGet("directions")]
    public async Task<IActionResult> GetDirections([FromQuery] double startLat, [FromQuery] double startLng, [FromQuery] double endLat, [FromQuery] double endLng)
    {
        try
        {
            // OSRM API call with steps
            var url = $"{_osrmBaseUrl}/route/v1/driving/{startLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{endLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=full&steps=true&geometries=geojson";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var osrmResponse = JsonSerializer.Deserialize<OsrmFullResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (osrmResponse?.Routes?.Any() == true)
            {
                var route = osrmResponse.Routes[0];
                var coordinates = route.Geometry?.Coordinates?.Select(coord => new { lat = coord[1], lng = coord[0] }).ToArray() ?? Array.Empty<object>();

                var steps = new List<object>();
                foreach (var leg in route.Legs)
                {
                    foreach (var step in leg.Steps)
                    {
                        steps.Add(new
                        {
                            instruction = step.Maneuver?.Instruction ?? "Continue",
                            distance = step.Distance,
                            duration = step.Duration,
                            type = step.Maneuver?.Type ?? "continue"
                        });
                    }
                }

                return Ok(new
                {
                    coordinates = coordinates,
                    steps = steps,
                    distance = route.Distance,
                    duration = route.Duration
                });
            }

            return BadRequest("No route found");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Routing service error: {ex.Message}");
        }
    }
}

// Extended OSRM models for full response
public class OsrmFullResponse
{
    public string Code { get; set; } = string.Empty;
    public List<OsrmFullRoute> Routes { get; set; } = new();
}

public class OsrmFullRoute
{
    public double Distance { get; set; }
    public double Duration { get; set; }
    public OsrmGeometry Geometry { get; set; } = new();
    public List<OsrmFullLeg> Legs { get; set; } = new();
}

public class OsrmGeometry
{
    public string Type { get; set; } = string.Empty;
    public List<double[]> Coordinates { get; set; } = new();
}

public class OsrmFullLeg
{
    public List<OsrmStep> Steps { get; set; } = new();
}

public class OsrmStep
{
    public double Distance { get; set; }
    public double Duration { get; set; }
    public OsrmManeuver Maneuver { get; set; } = new();
}

public class OsrmManeuver
{
    public string Type { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
}