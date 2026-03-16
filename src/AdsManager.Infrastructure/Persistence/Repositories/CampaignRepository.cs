using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class CampaignRepository : ICampaignRepository
{
    private readonly IApplicationDbContext _dbContext;

    public CampaignRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Campaign>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.Campaigns.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public Task<Campaign?> GetByIdAsync(Guid tenantId, Guid campaignId, CancellationToken cancellationToken = default)
        => _dbContext.Campaigns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == campaignId, cancellationToken);

    public Task<Campaign?> GetByMetaCampaignIdAsync(Guid tenantId, string metaCampaignId, CancellationToken cancellationToken = default)
        => _dbContext.Campaigns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaCampaignId == metaCampaignId, cancellationToken);

    public async Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        _dbContext.Campaigns.Update(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
