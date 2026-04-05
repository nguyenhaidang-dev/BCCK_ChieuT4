namespace backend.Models;

public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public int ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public int? TaskId { get; set; }
    public Task? Task { get; set; }
}