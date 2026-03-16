namespace AdsManager.Application.DTOs.Insights;

public sealed record TopCampaignDto(Guid CampaignId, string CampaignName, decimal Spend, long Clicks, decimal Ctr);

public sealed record DashboardTotalsDto(decimal TotalSpend, long TotalImpressions, long TotalClicks, decimal AverageCpc, decimal AverageCtr);

public sealed record DashboardDto(
    decimal TotalSpend,
    long TotalImpressions,
    long TotalClicks,
    decimal AverageCtr,
    decimal AverageCpc,
    decimal AverageCpm,
    IReadOnlyCollection<TopCampaignDto> TopCampaigns);

public sealed record DashboardFilter(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? CampaignId,
    Guid? AdAccountId);
