using AdsManager.Domain.Common;

namespace AdsManager.Domain.Entities;

public sealed class SyncJobRun : BaseEntity
{
    public string JobName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}
