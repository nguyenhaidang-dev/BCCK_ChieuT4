namespace backend.Models;

public class Feedback
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public FeedbackType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? Rating { get; set; } // 1-5 stars
    public FeedbackStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? Response { get; set; }
}

public enum FeedbackType
{
    AppFeedback,
    TaskComplaint,
    FeatureRequest,
    BugReport,
    General
}

public enum FeedbackStatus
{
    Pending,
    Reviewed,
    Responded,
    Closed
}