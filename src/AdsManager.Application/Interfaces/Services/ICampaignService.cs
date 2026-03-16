using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Campaigns;

namespace AdsManager.Application.Interfaces.Services;

public interface ICampaignService
{
    Task<Result<IReadOnlyCollection<CampaignDto>>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> GetByIdAsync(Guid tenantId, Guid campaignId, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> CreateAsync(Guid tenantId, CreateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> UpdateAsync(Guid tenantId, Guid campaignId, UpdateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> PauseAsync(Guid tenantId, Guid campaignId, Guid? userId, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> ActivateAsync(Guid tenantId, Guid campaignId, Guid? userId, CancellationToken cancellationToken = default);
}
