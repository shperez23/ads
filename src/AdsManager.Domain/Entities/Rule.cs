using AdsManager.Domain.Common;
using AdsManager.Domain.Enums;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class Rule : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RuleEntityLevel EntityLevel { get; set; }
    public RuleMetric Metric { get; set; }
    public RuleOperator Operator { get; set; }
    public decimal Threshold { get; set; }
    public RuleAction Action { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RuleExecutionLog> ExecutionLogs { get; set; } = new List<RuleExecutionLog>();
}
