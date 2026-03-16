using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class RuleRepository : IRuleRepository
{
    private readonly IApplicationDbContext _dbContext;

    public RuleRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Rule>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.Rules
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Rule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Rules
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Rule?> GetByIdAsync(Guid tenantId, Guid ruleId, CancellationToken cancellationToken = default)
        => _dbContext.Rules.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == ruleId, cancellationToken);

    public async Task AddAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        _dbContext.Rules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        _dbContext.Rules.Update(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddExecutionLogAsync(RuleExecutionLog log, CancellationToken cancellationToken = default)
    {
        _dbContext.RuleExecutionLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
