using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class AdSetRepository : IAdSetRepository
{
    private readonly IApplicationDbContext _dbContext;

    public AdSetRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AdSet>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.AdSets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<AdSet?> GetByIdAsync(Guid tenantId, Guid adSetId, CancellationToken cancellationToken = default)
        => _dbContext.AdSets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == adSetId, cancellationToken);

    public Task<AdSet?> GetByMetaAdSetIdAsync(Guid tenantId, string metaAdSetId, CancellationToken cancellationToken = default)
        => _dbContext.AdSets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaAdSetId == metaAdSetId, cancellationToken);

    public async Task AddAsync(AdSet adSet, CancellationToken cancellationToken = default)
    {
        _dbContext.AdSets.Add(adSet);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AdSet adSet, CancellationToken cancellationToken = default)
    {
        _dbContext.AdSets.Update(adSet);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
