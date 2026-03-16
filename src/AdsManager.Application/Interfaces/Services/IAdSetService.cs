using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdSets;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdSetService
{
    Task<Result<IReadOnlyCollection<AdSetDto>>> GetAdSetsAsync(CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> GetAdSetByIdAsync(Guid adSetId, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> CreateAsync(CreateAdSetRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> UpdateAdSetAsync(Guid adSetId, UpdateAdSetRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> PauseAdSetAsync(Guid adSetId, CancellationToken cancellationToken = default);
    Task<Result<AdSetDto>> ActivateAdSetAsync(Guid adSetId, CancellationToken cancellationToken = default);
}
