using AdsManager.Domain.Common;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class AdSet : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid CampaignId { get; set; }
    public string MetaAdSetId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public string BillingEvent { get; set; } = string.Empty;
    public string OptimizationGoal { get; set; } = string.Empty;
    public string BidStrategy { get; set; } = string.Empty;
    public string TargetingJson { get; set; } = "{}";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();
}
