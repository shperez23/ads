using AdsManager.Application.DTOs.AdSets;
using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IAdSetRepository
{
    Task<IReadOnlyCollection<AdSet>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<AdSet> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, AdSetListRequest request, CancellationToken cancellationToken = default);
    Task<AdSet?> GetByIdAsync(Guid tenantId, Guid adSetId, CancellationToken cancellationToken = default);
    Task<AdSet?> GetByMetaAdSetIdAsync(Guid tenantId, string metaAdSetId, CancellationToken cancellationToken = default);
    Task AddAsync(AdSet adSet, CancellationToken cancellationToken = default);
    Task UpdateAsync(AdSet adSet, CancellationToken cancellationToken = default);
}
