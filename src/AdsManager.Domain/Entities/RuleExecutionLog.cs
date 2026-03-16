using AdsManager.Domain.Common;
using AdsManager.Domain.Enums;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class RuleExecutionLog : BaseEntity, ITenantScoped
{
    public Guid RuleId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public decimal MetricValue { get; set; }
    public string ActionExecuted { get; set; } = string.Empty;
    public RuleExecutionStatus Status { get; set; }
    public string Details { get; set; } = string.Empty;

    public Rule Rule { get; set; } = null!;
}
