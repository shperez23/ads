using AdsManager.Domain.Common;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class Campaign : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AdAccountId { get; set; }
    public string MetaCampaignId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? DailyBudget { get; set; }
    public decimal? LifetimeBudget { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public AdAccount AdAccount { get; set; } = null!;
    public ICollection<AdSet> AdSets { get; set; } = new List<AdSet>();
}
