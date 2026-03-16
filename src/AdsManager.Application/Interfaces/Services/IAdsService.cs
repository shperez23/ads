using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Ads;
using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdsService
{
    Task<Result<PagedResponse<AdDto>>> GetAdsAsync(AdListRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> GetAdByIdAsync(Guid adId, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> CreateAdAsync(CreateAdRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> CreateAsync(CreateAdRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> UpdateAdAsync(Guid adId, UpdateAdRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> PauseAdAsync(Guid adId, CancellationToken cancellationToken = default);
    Task<Result<AdDto>> ActivateAdAsync(Guid adId, CancellationToken cancellationToken = default);
}
