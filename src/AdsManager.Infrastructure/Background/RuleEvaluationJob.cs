using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using AdsManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AdsManager.Infrastructure.Background;

public sealed class RuleEvaluationJob
{
    private const string PausedStatus = "PAUSED";

    private readonly IRuleRepository _ruleRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;
    private readonly IJobExecutionGuard _jobExecutionGuard;
    private readonly IObservabilityMetrics _observabilityMetrics;

    public RuleEvaluationJob(IRuleRepository ruleRepository, IApplicationDbContext dbContext, IMetaAdsService metaAdsService, IJobExecutionGuard jobExecutionGuard, IObservabilityMetrics observabilityMetrics)
    {
        _ruleRepository = ruleRepository;
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
        _jobExecutionGuard = jobExecutionGuard;
        _observabilityMetrics = observabilityMetrics;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetActiveRulesAsync(cancellationToken);
        var tenantIds = rules.Select(x => x.TenantId).Distinct().ToList();

        foreach (var tenantId in tenantIds)
        {
            var lease = await _jobExecutionGuard.TryStartAsync(nameof(RuleEvaluationJob), tenantId, null, cancellationToken);
            if (!lease.Acquired)
            {
                Log.Information("RuleEvaluationJob skipped for tenant {TenantId} due to active execution", tenantId);
                continue;
            }

            try
            {
                foreach (var rule in rules.Where(x => x.TenantId == tenantId))
                {
                    var candidates = await ResolveCandidatesAsync(rule, cancellationToken);

                    foreach (var candidate in candidates)
                    {
                        var shouldExecute = Evaluate(rule.Operator, candidate.MetricValue, rule.Threshold);
                        if (!shouldExecute)
                        {
                            await WriteLogAsync(rule, candidate, RuleExecutionStatus.Skipped, $"Condición no cumplida. MetricValue={candidate.MetricValue}", cancellationToken);
                            _observabilityMetrics.RecordRuleExecution(rule.Action.ToString(), RuleExecutionStatus.Skipped.ToString());
                            continue;
                        }

                        try
                        {
                            var details = await ExecuteActionAsync(rule, candidate, cancellationToken);
                            await WriteLogAsync(rule, candidate, RuleExecutionStatus.Success, details, cancellationToken);
                            _observabilityMetrics.RecordRuleExecution(rule.Action.ToString(), RuleExecutionStatus.Success.ToString());
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error ejecutando regla {RuleId} para entidad {EntityId}", rule.Id, candidate.EntityId);
                            await WriteLogAsync(rule, candidate, RuleExecutionStatus.Failed, ex.Message, cancellationToken);
                            _observabilityMetrics.RecordRuleExecution(rule.Action.ToString(), RuleExecutionStatus.Failed.ToString());
                        }
                    }
                }

                await _jobExecutionGuard.CompleteAsync(lease, SyncJobRunStatus.Succeeded, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _jobExecutionGuard.CompleteAsync(lease, SyncJobRunStatus.Failed, ex.Message, cancellationToken);
                throw;
            }
        }
    }

    private async Task<IReadOnlyCollection<RuleCandidate>> ResolveCandidatesAsync(Rule rule, CancellationToken cancellationToken)
    {
        if (rule.EntityLevel == RuleEntityLevel.Campaign)
        {
            var query = from insight in _dbContext.InsightsDaily.AsNoTracking()
                        join campaign in _dbContext.Campaigns.AsNoTracking() on insight.CampaignId equals campaign.Id
                        where insight.TenantId == rule.TenantId && insight.CampaignId.HasValue
                        select new { insight, campaign };

            return await query
                .GroupBy(x => x.campaign.Id)
                .Select(g => g.OrderByDescending(x => x.insight.Date).First())
                .Select(x => new RuleCandidate(x.campaign.Id, x.campaign.Name, ResolveMetricValue(rule.Metric, x.insight), x.campaign.MetaCampaignId, null, x.insight.TenantId))
                .ToListAsync(cancellationToken);
        }

        if (rule.EntityLevel == RuleEntityLevel.AdSet)
        {
            var query = from insight in _dbContext.InsightsDaily.AsNoTracking()
                        join adSet in _dbContext.AdSets.AsNoTracking() on insight.AdSetId equals adSet.Id
                        where insight.TenantId == rule.TenantId && insight.AdSetId.HasValue
                        select new { insight, adSet };

            return await query
                .GroupBy(x => x.adSet.Id)
                .Select(g => g.OrderByDescending(x => x.insight.Date).First())
                .Select(x => new RuleCandidate(x.adSet.Id, x.adSet.Name, ResolveMetricValue(rule.Metric, x.insight), null, x.adSet.MetaAdSetId, x.insight.TenantId))
                .ToListAsync(cancellationToken);
        }

        if (rule.EntityLevel == RuleEntityLevel.Account)
        {
            return await _dbContext.InsightsDaily.AsNoTracking()
                .Where(x => x.TenantId == rule.TenantId)
                .GroupBy(x => x.AdAccountId)
                .Select(g => g.OrderByDescending(x => x.Date).First())
                .Select(x => new RuleCandidate(x.AdAccountId, "AdAccount", ResolveMetricValue(rule.Metric, x), null, null, x.TenantId))
                .ToListAsync(cancellationToken);
        }

        return [];
    }

    private async Task<string> ExecuteActionAsync(Rule rule, RuleCandidate candidate, CancellationToken cancellationToken)
    {
        return rule.Action switch
        {
            RuleAction.PauseCampaign when !string.IsNullOrWhiteSpace(candidate.MetaCampaignId) => await PauseCampaignAsync(candidate, cancellationToken),
            RuleAction.PauseAdSet when !string.IsNullOrWhiteSpace(candidate.MetaAdSetId) => await PauseAdSetAsync(candidate, cancellationToken),
            RuleAction.Alert => $"Alerta generada para {candidate.EntityName} ({candidate.EntityId})",
            _ => "Acción no compatible con entidad evaluada"
        };
    }

    private async Task<string> PauseCampaignAsync(RuleCandidate candidate, CancellationToken cancellationToken)
    {
        await _metaAdsService.UpdateCampaignStatusAsync(candidate.TenantId, new(candidate.MetaCampaignId!, PausedStatus), cancellationToken);
        return $"Campaña pausada ({candidate.EntityName})";
    }

    private async Task<string> PauseAdSetAsync(RuleCandidate candidate, CancellationToken cancellationToken)
    {
        await _metaAdsService.UpdateAdSetStatusAsync(candidate.TenantId, new(candidate.MetaAdSetId!, PausedStatus), cancellationToken);
        return $"AdSet pausado ({candidate.EntityName})";
    }

    private async Task WriteLogAsync(Rule rule, RuleCandidate candidate, RuleExecutionStatus status, string details, CancellationToken cancellationToken)
    {
        await _ruleRepository.AddExecutionLogAsync(new RuleExecutionLog
        {
            RuleId = rule.Id,
            TenantId = rule.TenantId,
            ExecutedAt = DateTime.UtcNow,
            EntityName = candidate.EntityName,
            EntityId = candidate.EntityId,
            MetricValue = candidate.MetricValue,
            ActionExecuted = rule.Action.ToString(),
            Status = status,
            Details = details
        }, cancellationToken);
    }

    private static decimal ResolveMetricValue(RuleMetric metric, InsightDaily insight)
        => metric switch
        {
            RuleMetric.Ctr => insight.Ctr,
            RuleMetric.Cpc => insight.Cpc,
            RuleMetric.Spend => insight.Spend,
            _ => 0m
        };

    private static bool Evaluate(RuleOperator @operator, decimal metricValue, decimal threshold)
        => @operator switch
        {
            RuleOperator.LessThan => metricValue < threshold,
            RuleOperator.GreaterThan => metricValue > threshold,
            _ => false
        };

    private sealed record RuleCandidate(Guid EntityId, string EntityName, decimal MetricValue, string? MetaCampaignId, string? MetaAdSetId, Guid TenantId);
}
