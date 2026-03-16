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
