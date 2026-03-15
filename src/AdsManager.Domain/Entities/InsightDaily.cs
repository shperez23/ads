using AdsManager.Domain.Common;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class InsightDaily : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AdAccountId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? AdSetId { get; set; }
    public Guid? AdId { get; set; }
    public DateOnly Date { get; set; }
    public long Impressions { get; set; }
    public long Reach { get; set; }
    public long Clicks { get; set; }
    public long LinkClicks { get; set; }
    public decimal Spend { get; set; }
    public decimal Cpm { get; set; }
    public decimal Cpc { get; set; }
    public decimal Ctr { get; set; }
    public string ConversionsJson { get; set; } = "[]";
}
