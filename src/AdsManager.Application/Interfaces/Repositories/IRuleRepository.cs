using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IRuleRepository
{
    Task<IReadOnlyCollection<Rule>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Rule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    Task<Rule?> GetByIdAsync(Guid tenantId, Guid ruleId, CancellationToken cancellationToken = default);
    Task AddAsync(Rule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default);
    Task AddExecutionLogAsync(RuleExecutionLog log, CancellationToken cancellationToken = default);
}
