using AdsManager.Application.DTOs.Common;
using AdsManager.Domain.Enums;

namespace AdsManager.Application.DTOs.Rules;

public sealed record RuleDto(
    Guid Id,
    string Name,
    RuleEntityLevel EntityLevel,
    RuleMetric Metric,
    RuleOperator Operator,
    decimal Threshold,
    RuleAction Action,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateRuleRequest(
    string Name,
    RuleEntityLevel EntityLevel,
    RuleMetric Metric,
    RuleOperator Operator,
    decimal Threshold,
    RuleAction Action,
    bool IsActive);

public sealed record UpdateRuleRequest(
    string Name,
    RuleEntityLevel EntityLevel,
    RuleMetric Metric,
    RuleOperator Operator,
    decimal Threshold,
    RuleAction Action,
    bool IsActive);

public sealed record RuleListRequest : PagedRequest
{
    public bool? Status { get; init; }
}
