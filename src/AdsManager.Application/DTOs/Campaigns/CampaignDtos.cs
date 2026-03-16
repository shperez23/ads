using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.DTOs.Campaigns;

public sealed record CampaignDto(
    Guid Id,
    string MetaCampaignId,
    Guid AdAccountId,
    string Name,
    string Objective,
    string Status,
    decimal? DailyBudget,
    decimal? LifetimeBudget,
    DateTime? StartDate,
    DateTime? EndDate);

public sealed record CreateCampaignRequest(
    Guid AdAccountId,
    string Name,
    string Objective,
    string Status,
    long? DailyBudget,
    long? LifetimeBudget);

public sealed record UpdateCampaignRequest(
    string Name,
    string Objective,
    string Status,
    decimal? DailyBudget,
    decimal? LifetimeBudget,
    DateTime? StartDate,
    DateTime? EndDate);

public sealed record CampaignListRequest : PagedRequest
{
    public string? Status { get; init; }
    public Guid? AdAccountId { get; init; }
}
