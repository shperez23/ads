using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.DTOs.AdSets;

public sealed record AdSetDto(
    Guid Id,
    string MetaAdSetId,
    Guid CampaignId,
    string Name,
    string Status,
    decimal Budget,
    string BillingEvent,
    string OptimizationGoal,
    string BidStrategy,
    string TargetingJson,
    DateTime? StartDate,
    DateTime? EndDate);

public sealed record CreateAdSetRequest(
    Guid CampaignId,
    string Name,
    string Status,
    long DailyBudget,
    string BillingEvent,
    string OptimizationGoal,
    string TargetingJson,
    string BidStrategy = "LOWEST_COST_WITHOUT_CAP");

public sealed record UpdateAdSetRequest(
    string Name,
    string Status,
    decimal Budget,
    string BillingEvent,
    string OptimizationGoal,
    string TargetingJson,
    string BidStrategy,
    DateTime? StartDate,
    DateTime? EndDate);

public sealed record AdSetListRequest : PagedRequest
{
    public string? Status { get; init; }
    public Guid? CampaignId { get; init; }
}
