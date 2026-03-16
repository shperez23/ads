using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IAdRepository
{
    Task<Ad?> GetByIdAsync(Guid tenantId, Guid adId, CancellationToken cancellationToken = default);
    Task AddAsync(Ad ad, CancellationToken cancellationToken = default);
}
