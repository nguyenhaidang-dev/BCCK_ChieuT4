using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagesController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: api/Messages
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetMyMessages([FromQuery] int? withUserId = null)
    {
        var currentUserIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out var currentUserId))
        {
            return Unauthorized();
        }

        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Task)
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId);

        if (withUserId.HasValue)
        {
            query = query.Where(m => (m.SenderId == currentUserId && m.ReceiverId == withUserId) ||
                                     (m.SenderId == withUserId && m.ReceiverId == currentUserId));
        }

        var messages = await query.OrderBy(m => m.SentAt).ToListAsync();
        return Ok(messages);
    }

    // POST: api/Messages
    [HttpPost]
    public async Task<ActionResult<Message>> SendMessage([FromBody] SendMessageRequest request)
    {
        var currentUserIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out var currentUserId))
        {
            return Unauthorized();
        }

        // Validate receiver exists
        var receiver = await _userManager.FindByIdAsync(request.ReceiverId.ToString());
        if (receiver == null)
        {
            return BadRequest("Receiver not found");
        }

        var message = new Message
        {
            SenderId = currentUserId,
            ReceiverId = request.ReceiverId,
            Content = request.Content,
            SentAt = DateTime.Now,
            IsRead = false,
            TaskId = request.TaskId
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
        await _context.Entry(message).Reference(m => m.Receiver).LoadAsync();
        if (message.TaskId.HasValue)
        {
            await _context.Entry(message).Reference(m => m.Task).LoadAsync();
        }

        return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
    }

    // GET: api/Messages/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetMessage(int id)
    {
        var currentUserIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out var currentUserId))
        {
            return Unauthorized();
        }

        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Task)
            .FirstOrDefaultAsync(m => m.Id == id &&
                                    (m.SenderId == currentUserId || m.ReceiverId == currentUserId));

        if (message == null)
        {
            return NotFound();
        }

        // Mark as read if current user is receiver
        if (message.ReceiverId == currentUserId && !message.IsRead)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return Ok(message);
    }

    // PUT: api/Messages/5/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var currentUserIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out var currentUserId))
        {
            return Unauthorized();
        }

        var message = await _context.Messages.FindAsync(id);
        if (message == null || message.ReceiverId != currentUserId)
        {
            return NotFound();
        }

        message.IsRead = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/Messages/conversations
    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<ConversationSummary>>> GetConversations()
    {
        var currentUserIdStr = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out var currentUserId))
        {
            return Unauthorized();
        }

        var conversations = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
            .Select(g => new ConversationSummary
            {
                OtherUserId = g.Key,
                OtherUserName = g.First().SenderId == currentUserId ?
                    (g.First().Receiver != null ? g.First().Receiver.Name : string.Empty) : (g.First().Sender != null ? g.First().Sender.Name : string.Empty),
                LastMessage = g.OrderByDescending(m => m.SentAt).First().Content,
                LastMessageTime = g.Max(m => m.SentAt),
                UnreadCount = g.Count(m => !m.IsRead && m.ReceiverId == currentUserId)
            })
            .OrderByDescending(c => c.LastMessageTime)
            .ToListAsync();

        return Ok(conversations);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}

public class SendMessageRequest
{
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? TaskId { get; set; }
}

public class ConversationSummary
{
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
}
// Added logging mechanism.
