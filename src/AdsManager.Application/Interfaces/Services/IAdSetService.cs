using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdSets;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdSetService
{
    Task<Result<AdSetDto>> CreateAsync(Guid tenantId, CreateAdSetRequest request, CancellationToken cancellationToken = default);
}
