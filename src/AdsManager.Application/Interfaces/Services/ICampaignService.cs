using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Campaigns;
using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.Interfaces.Services;

public interface ICampaignService
{
    Task<Result<PagedResponse<CampaignDto>>> GetAllAsync(CampaignListRequest request, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> CreateAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> UpdateAsync(Guid campaignId, UpdateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> PauseAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Result<CampaignDto>> ActivateAsync(Guid campaignId, CancellationToken cancellationToken = default);
}
