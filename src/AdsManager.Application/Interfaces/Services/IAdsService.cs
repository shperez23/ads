using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Ads;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdsService
{
    Task<Result<IReadOnlyCollection<AdDto>>> GetAdsAsync(CancellationToken cancellationToken = default);
    Task<Result<AdDto>> GetAdByIdAsync(Guid adId, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> CreateAsync(CreateAdRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> UpdateAdAsync(Guid adId, UpdateAdRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> PauseAdAsync(Guid adId, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> ActivateAdAsync(Guid adId, CancellationToken cancellationToken = default);
}
