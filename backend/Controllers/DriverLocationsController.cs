using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[Authorize(Roles = "Admin,Manager")]
[ApiController]
[Route("api/[controller]")]
public class DriverLocationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DriverLocationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/driverlocations/{driverId}
    [HttpGet("{driverId}")]
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

        return Ok(new
        {
            latitude = location.Latitude,
            longitude = location.Longitude,
            timestamp = location.Timestamp
        });
    }
}