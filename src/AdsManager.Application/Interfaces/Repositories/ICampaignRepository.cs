using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface ICampaignRepository
{
    Task<IReadOnlyCollection<Campaign>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Campaign?> GetByIdAsync(Guid tenantId, Guid campaignId, CancellationToken cancellationToken = default);
    Task<Campaign?> GetByMetaCampaignIdAsync(Guid tenantId, string metaCampaignId, CancellationToken cancellationToken = default);
    Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default);
    Task UpdateAsync(Campaign campaign, CancellationToken cancellationToken = default);
}
