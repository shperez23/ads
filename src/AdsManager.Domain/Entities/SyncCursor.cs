using AdsManager.Domain.Common;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class SyncCursor : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string AdAccountId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public DateTime LastSyncedAt { get; set; }
}
