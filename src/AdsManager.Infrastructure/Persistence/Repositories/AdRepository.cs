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

    public Task<Ad?> GetByIdAsync(Guid tenantId, Guid adId, CancellationToken cancellationToken = default)
        => _dbContext.Ads.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == adId, cancellationToken);

    public async Task AddAsync(Ad ad, CancellationToken cancellationToken = default)
    {
        _dbContext.Ads.Add(ad);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
