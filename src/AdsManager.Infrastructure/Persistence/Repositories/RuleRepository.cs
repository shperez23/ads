using AdsManager.Application.DTOs.Common;
using AdsManager.Application.DTOs.Rules;
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

    public async Task<(IReadOnlyCollection<Rule> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, RuleListRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Rules.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search));
        }

        if (request.Status.HasValue)
            query = query.Where(x => x.IsActive == request.Status.Value);

        query = ApplySorting(query, request.SortBy, request.SortDirection);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.NormalizedPage - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

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

    private static IQueryable<Rule> ApplySorting(IQueryable<Rule> query, string? sortBy, SortDirection sortDirection)
    {
        var desc = sortDirection == SortDirection.Desc;

        return sortBy?.ToLowerInvariant() switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => desc ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }
}
