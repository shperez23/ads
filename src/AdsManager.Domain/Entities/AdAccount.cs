using AdsManager.Domain.Common;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class AdAccount : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string MetaAccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string TimezoneName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
}
