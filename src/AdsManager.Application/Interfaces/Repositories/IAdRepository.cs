using AdsManager.Application.DTOs.Ads;
using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IAdRepository
{
    Task<IReadOnlyCollection<Ad>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<Ad> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, AdListRequest request, CancellationToken cancellationToken = default);
    Task<Ad?> GetByIdAsync(Guid tenantId, Guid adId, CancellationToken cancellationToken = default);
    Task AddAsync(Ad ad, CancellationToken cancellationToken = default);
    Task UpdateAsync(Ad ad, CancellationToken cancellationToken = default);
}
