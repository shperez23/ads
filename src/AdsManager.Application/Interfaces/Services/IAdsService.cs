using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Ads;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdsService
{
    Task<Result<AdDto>> CreateAsync(Guid tenantId, CreateAdRequest request, CancellationToken cancellationToken = default);
}
