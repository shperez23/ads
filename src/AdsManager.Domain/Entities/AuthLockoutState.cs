using AdsManager.Domain.Common;

namespace AdsManager.Domain.Entities;

public sealed class AuthLockoutState : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime? LastFailedAt { get; set; }
    public DateTime? LockoutUntil { get; set; }
}
