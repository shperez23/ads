using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IMetaConnectionRepository
{
    Task<IReadOnlyCollection<MetaConnection>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<MetaConnection?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MetaConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(MetaConnection connection, CancellationToken cancellationToken = default);
    Task DeleteAsync(MetaConnection connection, CancellationToken cancellationToken = default);
}
