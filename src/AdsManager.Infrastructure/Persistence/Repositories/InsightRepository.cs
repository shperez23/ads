using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class InsightRepository : IInsightRepository
{
    private readonly IApplicationDbContext _dbContext;

    public InsightRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<InsightDaily>> GetByDateRangeAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await _dbContext.InsightsDaily
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Date >= from && x.Date <= to)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<InsightDaily> insights, CancellationToken cancellationToken = default)
    {
        _dbContext.InsightsDaily.AddRange(insights);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
