namespace AdsManager.Application.DTOs.Meta;

public sealed record MetaAdAccountDto(string Id, string Name, string AccountStatus, string Currency, string TimezoneName);
public sealed record MetaCampaignDto(string Id, string Name, string Status, string Objective);
public sealed record MetaAdSetDto(string Id, string CampaignId, string Name, string Status, decimal DailyBudget, string BillingEvent, string OptimizationGoal, string TargetingJson);
public sealed record MetaAdDto(string Id, string AdSetId, string Name, string Status, string CreativeJson);
public sealed record MetaResourceIdentifierDto(string Id);
public sealed record MetaCampaignCreateRequest(string Name, string Objective, string Status, long? DailyBudget, long? LifetimeBudget);
public sealed record MetaCampaignStatusUpdateRequest(string CampaignId, string Status);
public sealed record MetaAdSetCreateRequest(string Name, string CampaignId, string Status, long DailyBudget, string BillingEvent, string OptimizationGoal, string TargetingJson);
public sealed record MetaAdSetUpdateRequest(string AdSetId, string Name, string Status, decimal DailyBudget, string BillingEvent, string OptimizationGoal, string TargetingJson);
public sealed record MetaAdSetStatusUpdateRequest(string AdSetId, string Status);
public sealed record MetaAdCreateRequest(string Name, string AdSetId, string Status, string CreativeJson);
public sealed record MetaAdUpdateRequest(string AdId, string Name, string Status, string CreativeJson);
public sealed record MetaAdStatusUpdateRequest(string AdId, string Status);
public sealed record MetaInsightDto(
    string DateStart,
    string DateStop,
    string CampaignId,
    string CampaignName,
    string AdSetId,
    string AdSetName,
    string AdId,
    string AdName,
    string Spend,
    string Impressions,
    string Reach,
    string Clicks,
    string LinkClicks,
    string Ctr,
    string Cpc,
    string Cpm);
