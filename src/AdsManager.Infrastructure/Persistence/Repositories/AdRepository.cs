using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class AdRepository : IAdRepository
{
    private readonly IApplicationDbContext _dbContext;

    public AdRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Ad>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.Ads
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Ad?> GetByIdAsync(Guid tenantId, Guid adId, CancellationToken cancellationToken = default)
        => _dbContext.Ads.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == adId, cancellationToken);

    public async Task AddAsync(Ad ad, CancellationToken cancellationToken = default)
    {
        _dbContext.Ads.Add(ad);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ad ad, CancellationToken cancellationToken = default)
    {
        _dbContext.Ads.Update(ad);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
