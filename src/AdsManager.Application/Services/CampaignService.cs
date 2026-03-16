using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Campaigns;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AdsManager.Application.Services;

public sealed class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IObservabilityMetrics _observabilityMetrics;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;

    public CampaignService(ICampaignRepository campaignRepository, IApplicationDbContext dbContext, IMetaAdsService metaAdsService, ITenantProvider tenantProvider, IObservabilityMetrics observabilityMetrics, IAuditService auditService, ICacheService cacheService)
    {
        _campaignRepository = campaignRepository;
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
        _tenantProvider = tenantProvider;
        _observabilityMetrics = observabilityMetrics;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public async Task<Result<IReadOnlyCollection<CampaignDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<IReadOnlyCollection<CampaignDto>>.Fail("Tenant no resuelto");

        var campaigns = await _campaignRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<CampaignDto>>.Ok(campaigns.Select(Map).ToArray());
    }

    public async Task<Result<CampaignDto>> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<CampaignDto>.Fail("Tenant no resuelto");

        var campaign = await _campaignRepository.GetByIdAsync(tenantId, campaignId, cancellationToken);
        return campaign is null
            ? Result<CampaignDto>.Fail("Campaña no encontrada")
            : Result<CampaignDto>.Ok(Map(campaign));
    }

    public async Task<Result<CampaignDto>> CreateAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<CampaignDto>.Fail("Tenant no resuelto");

        var adAccount = await _dbContext.AdAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AdAccountId, cancellationToken);

        if (adAccount is null)
            return Result<CampaignDto>.Fail("Ad account no encontrada para el tenant");

        var metaId = await _metaAdsService.CreateCampaignAsync(tenantId, adAccount.MetaAccountId,
            new MetaCampaignCreateRequest(request.Name, request.Objective, request.Status, request.DailyBudget, request.LifetimeBudget),
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
        await InvalidateInsightsCacheAsync(tenantId, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "create campaign", nameof(Campaign), campaign.Id.ToString(), JsonSerializer.Serialize(campaign), cancellationToken);
        _observabilityMetrics.IncrementCampaignCreation();
        return Result<CampaignDto>.Ok(Map(campaign), "Campaña creada correctamente");
    }

    public async Task<Result<CampaignDto>> UpdateAsync(Guid campaignId, UpdateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<CampaignDto>.Fail("Tenant no resuelto");

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
        await InvalidateInsightsCacheAsync(tenantId, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "update campaign", nameof(Campaign), campaign.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken);
        return Result<CampaignDto>.Ok(Map(campaign), "Campaña actualizada correctamente");
    }

    public Task<Result<CampaignDto>> PauseAsync(Guid campaignId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(campaignId, "PAUSED", cancellationToken);

    public Task<Result<CampaignDto>> ActivateAsync(Guid campaignId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(campaignId, "ACTIVE", cancellationToken);

    private async Task<Result<CampaignDto>> ChangeStatusAsync(Guid campaignId, string status, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<CampaignDto>.Fail("Tenant no resuelto");

        var campaign = await _campaignRepository.GetByIdAsync(tenantId, campaignId, cancellationToken);
        if (campaign is null)
            return Result<CampaignDto>.Fail("Campaña no encontrada");

        await _metaAdsService.UpdateCampaignStatusAsync(tenantId, new MetaCampaignStatusUpdateRequest(campaign.MetaCampaignId, status), cancellationToken);

        campaign.Status = status;
        await _campaignRepository.UpdateAsync(campaign, cancellationToken);
        await InvalidateInsightsCacheAsync(tenantId, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, status == "PAUSED" ? "pause campaign" : "activate campaign", nameof(Campaign), campaign.Id.ToString(), JsonSerializer.Serialize(new { campaign.Status }), cancellationToken);

        return Result<CampaignDto>.Ok(Map(campaign), $"Estado actualizado a {status}");
    }

    private async Task InvalidateInsightsCacheAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        foreach (var prefix in InsightsCacheKeys.TenantPrefixes(tenantId))
            await _cacheService.RemoveByPrefixAsync(prefix, cancellationToken);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        return tenantId != Guid.Empty;
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
