using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Campaigns;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Application.Services;

public sealed class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;

    public CampaignService(ICampaignRepository campaignRepository, IApplicationDbContext dbContext, IMetaAdsService metaAdsService)
    {
        _campaignRepository = campaignRepository;
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
    }

    public async Task<Result<IReadOnlyCollection<CampaignDto>>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var campaigns = await _campaignRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<CampaignDto>>.Ok(campaigns.Select(Map).ToArray());
    }

    public async Task<Result<CampaignDto>> GetByIdAsync(Guid tenantId, Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(tenantId, campaignId, cancellationToken);
        return campaign is null
            ? Result<CampaignDto>.Fail("Campaña no encontrada")
            : Result<CampaignDto>.Ok(Map(campaign));
    }

    public async Task<Result<CampaignDto>> CreateAsync(Guid tenantId, CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var adAccount = await _dbContext.AdAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == request.AdAccountId, cancellationToken);

        if (adAccount is null)
            return Result<CampaignDto>.Fail("Ad account no encontrada para el tenant");

        var metaId = await _metaAdsService.CreateCampaignAsync(adAccount.MetaAccountId,
            new MetaCampaignCreateRequest(request.Name, request.Objective, request.Status, request.DailyBudget, request.LifetimeBudget),
            request.AccessToken,
            cancellationToken);

        var campaign = new Campaign
        {
            TenantId = tenantId,
            AdAccountId = request.AdAccountId,
            MetaCampaignId = metaId,
            Name = request.Name,
            Objective = request.Objective,
            Status = request.Status,
            DailyBudget = request.DailyBudget,
            LifetimeBudget = request.LifetimeBudget
        };

        await _campaignRepository.AddAsync(campaign, cancellationToken);
        return Result<CampaignDto>.Ok(Map(campaign), "Campaña creada correctamente");
    }

    public async Task<Result<CampaignDto>> UpdateAsync(Guid tenantId, Guid campaignId, UpdateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(tenantId, campaignId, cancellationToken);
        if (campaign is null)
            return Result<CampaignDto>.Fail("Campaña no encontrada");

        campaign.Name = request.Name;
        campaign.Objective = request.Objective;
        campaign.Status = request.Status;
        campaign.DailyBudget = request.DailyBudget;
        campaign.LifetimeBudget = request.LifetimeBudget;
        campaign.StartDate = request.StartDate;
        campaign.EndDate = request.EndDate;

        await _campaignRepository.UpdateAsync(campaign, cancellationToken);
        return Result<CampaignDto>.Ok(Map(campaign), "Campaña actualizada correctamente");
    }

    public Task<Result<CampaignDto>> PauseAsync(Guid tenantId, Guid campaignId, string accessToken, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(tenantId, campaignId, "PAUSED", accessToken, cancellationToken);

    public Task<Result<CampaignDto>> ActivateAsync(Guid tenantId, Guid campaignId, string accessToken, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(tenantId, campaignId, "ACTIVE", accessToken, cancellationToken);

    private async Task<Result<CampaignDto>> ChangeStatusAsync(Guid tenantId, Guid campaignId, string status, string accessToken, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(tenantId, campaignId, cancellationToken);
        if (campaign is null)
            return Result<CampaignDto>.Fail("Campaña no encontrada");

        await _metaAdsService.UpdateCampaignStatusAsync(new MetaCampaignStatusUpdateRequest(campaign.MetaCampaignId, status), accessToken, cancellationToken);

        campaign.Status = status;
        await _campaignRepository.UpdateAsync(campaign, cancellationToken);

        return Result<CampaignDto>.Ok(Map(campaign), $"Estado actualizado a {status}");
    }

    private static CampaignDto Map(Campaign campaign) =>
        new(
            campaign.Id,
            campaign.MetaCampaignId,
            campaign.AdAccountId,
            campaign.Name,
            campaign.Objective,
            campaign.Status,
            campaign.DailyBudget,
            campaign.LifetimeBudget,
            campaign.StartDate,
            campaign.EndDate);
}
