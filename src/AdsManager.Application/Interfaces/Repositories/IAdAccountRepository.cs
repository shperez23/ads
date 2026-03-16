using AdsManager.Application.DTOs.AdAccounts;
using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IAdAccountRepository
{
    Task<IReadOnlyCollection<AdAccount>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<AdAccount> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, AdAccountListRequest request, CancellationToken cancellationToken = default);
    Task<AdAccount?> GetByIdAsync(Guid tenantId, Guid adAccountId, CancellationToken cancellationToken = default);
    Task<AdAccount?> GetByMetaAccountIdAsync(Guid tenantId, string metaAccountId, CancellationToken cancellationToken = default);
    Task AddAsync(AdAccount adAccount, CancellationToken cancellationToken = default);
    Task UpdateAsync(AdAccount adAccount, CancellationToken cancellationToken = default);
}
