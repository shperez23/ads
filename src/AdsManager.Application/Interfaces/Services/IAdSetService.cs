using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdSets;
using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdSetService
{
    Task<Result<PagedResponse<AdSetDto>>> GetAdSetsAsync(AdSetListRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> GetAdSetByIdAsync(Guid adSetId, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> CreateAdSetAsync(CreateAdSetRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> CreateAsync(CreateAdSetRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> UpdateAdSetAsync(Guid adSetId, UpdateAdSetRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> PauseAdSetAsync(Guid adSetId, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> ActivateAdSetAsync(Guid adSetId, CancellationToken cancellationToken = default);
}
