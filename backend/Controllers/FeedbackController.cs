using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _context;

    public FeedbackController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/Feedback
    [HttpPost]
    public async Task<ActionResult<Feedback>> SubmitFeedback([FromBody] SubmitFeedbackRequest request)
    {
        var userIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var feedback = new Feedback
        {
            UserId = userId,
            Type = request.Type,
            Subject = request.Subject,
            Message = request.Message,
            Rating = request.Rating,
            Status = FeedbackStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFeedback), new { id = feedback.Id }, feedback);
    }

    // GET: api/Feedback
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Feedback>>> GetMyFeedback()
    {
        var userIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var feedbacks = await _context.Feedbacks
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return Ok(feedbacks);
    }

    // GET: api/Feedback/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Feedback>> GetFeedback(int id)
    {
        var userIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var feedback = await _context.Feedbacks
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

        if (feedback == null)
        {
            return NotFound();
        }

        return Ok(feedback);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}

public class SubmitFeedbackRequest
{
    public FeedbackType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? Rating { get; set; }
}