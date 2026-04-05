using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
[ApiController]
[Route("api/[controller]")]
public class DriversController : ControllerBase
{
    private readonly AppDbContext _context;

    public DriversController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/drivers/nearby
    [HttpGet("nearby")]
    public async Task<ActionResult<IEnumerable<User>>> GetNearbyDrivers(
        [FromQuery] decimal lat,
        [FromQuery] decimal lng,
        [FromQuery] decimal radiusKm = 10)
    {
        // For now, return all active drivers
        // In production, implement proper geospatial queries
        var drivers = await _context.Users
            .Where(u => u.Role == Role.Driver && u.Status == UserStatus.Active)
            .ToListAsync();

        return drivers;
    }

    // GET: api/drivers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetDrivers()
    {
        return await _context.Users
            .Where(u => u.Role == Role.Driver)
            .ToListAsync();
    }

    // GET: api/drivers/available
    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<User>>> GetAvailableDrivers()
    {
        return await _context.Users
            .Where(u => u.Role == Role.Driver && u.Status == UserStatus.Active)
            .ToListAsync();
    }
}