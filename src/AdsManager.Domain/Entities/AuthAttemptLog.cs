using AdsManager.Domain.Common;

namespace AdsManager.Domain.Entities;

public sealed class AuthAttemptLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
    public bool Success { get; set; }
    public string AttemptType { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}
