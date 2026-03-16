using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.DTOs.Insights;

public sealed record InsightDto(
    Guid Id,
    Guid AdAccountId,
    Guid? CampaignId,
    Guid? AdSetId,
    Guid? AdId,
    DateOnly Date,
    long Impressions,
    long Reach,
    long Clicks,
    long LinkClicks,
    decimal Spend,
    decimal Cpm,
    decimal Cpc,
    decimal Ctr);

public sealed record InsightListRequest : PagedRequest
{
    public Guid? CampaignId { get; init; }
    public Guid? AdAccountId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}
