using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IAdSetRepository
{
    Task<AdSet?> GetByIdAsync(Guid tenantId, Guid adSetId, CancellationToken cancellationToken = default);
    Task<AdSet?> GetByMetaAdSetIdAsync(Guid tenantId, string metaAdSetId, CancellationToken cancellationToken = default);
    Task AddAsync(AdSet adSet, CancellationToken cancellationToken = default);
}
