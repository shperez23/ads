using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdSets;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AdsManager.Application.Services;

public sealed class AdSetService : IAdSetService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IAdSetRepository _adSetRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;

    public AdSetService(ICampaignRepository campaignRepository, IAdSetRepository adSetRepository, IApplicationDbContext dbContext, IMetaAdsService metaAdsService, ITenantProvider tenantProvider, IAuditService auditService, ICacheService cacheService)
    {
        _campaignRepository = campaignRepository;
        _adSetRepository = adSetRepository;
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
        _tenantProvider = tenantProvider;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public async Task<Result<IReadOnlyCollection<AdSetDto>>> GetAdSetsAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<IReadOnlyCollection<AdSetDto>>.Fail("Tenant no resuelto");

        var adSets = await _adSetRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<AdSetDto>>.Ok(adSets.Select(Map).ToArray());
    }

    public async Task<Result<AdSetDto>> GetAdSetByIdAsync(Guid adSetId, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdSetDto>.Fail("Tenant no resuelto");

        var adSet = await _adSetRepository.GetByIdAsync(tenantId, adSetId, cancellationToken);
        return adSet is null
            ? Result<AdSetDto>.Fail("AdSet no encontrado")
            : Result<AdSetDto>.Ok(Map(adSet));
    }

    public Task<Result<AdSetDto>> CreateAdSetAsync(CreateAdSetRequest request, CancellationToken cancellationToken = default)
        => CreateAsync(request, cancellationToken);

    public async Task<Result<AdSetDto>> CreateAsync(CreateAdSetRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdSetDto>.Fail("Tenant no resuelto");

        var campaign = await _campaignRepository.GetByIdAsync(tenantId, request.CampaignId, cancellationToken);
        if (campaign is null)
            return Result<AdSetDto>.Fail("Campaña no encontrada");

        var adAccount = await _dbContext.AdAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == campaign.AdAccountId, cancellationToken);

        if (adAccount is null)
            return Result<AdSetDto>.Fail("Ad account no encontrada");

        var metaAdSetId = await _metaAdsService.CreateAdSetAsync(tenantId, adAccount.MetaAccountId,
            new MetaAdSetCreateRequest(request.Name, campaign.MetaCampaignId, request.Status, request.DailyBudget, request.BillingEvent, request.OptimizationGoal, request.TargetingJson),
            cancellationToken);

        var adSet = new AdSet
        {
            TenantId = tenantId,
            CampaignId = campaign.Id,
            MetaAdSetId = metaAdSetId,
            Name = request.Name,
            Status = request.Status,
            Budget = request.DailyBudget,
            BillingEvent = request.BillingEvent,
            OptimizationGoal = request.OptimizationGoal,
            BidStrategy = request.BidStrategy,
            TargetingJson = request.TargetingJson
        };

        await _adSetRepository.AddAsync(adSet, cancellationToken);
        await InvalidateInsightsCacheAsync(tenantId, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "create adset", nameof(AdSet), adSet.Id.ToString(), JsonSerializer.Serialize(adSet), cancellationToken);

        return Result<AdSetDto>.Ok(Map(adSet), "AdSet creado correctamente");
    }

    public async Task<Result<AdSetDto>> UpdateAdSetAsync(Guid adSetId, UpdateAdSetRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdSetDto>.Fail("Tenant no resuelto");

        var adSet = await _adSetRepository.GetByIdAsync(tenantId, adSetId, cancellationToken);
        if (adSet is null)
            return Result<AdSetDto>.Fail("AdSet no encontrado");

        await _metaAdsService.UpdateAdSetAsync(
            tenantId,
            new MetaAdSetUpdateRequest(adSet.MetaAdSetId, request.Name, request.Status, request.Budget, request.BillingEvent, request.OptimizationGoal, request.TargetingJson),
            cancellationToken);

        adSet.Name = request.Name;
        adSet.Status = request.Status;
        adSet.Budget = request.Budget;
        adSet.BillingEvent = request.BillingEvent;
        adSet.OptimizationGoal = request.OptimizationGoal;
        adSet.TargetingJson = request.TargetingJson;
        adSet.BidStrategy = request.BidStrategy;
        adSet.StartDate = request.StartDate;
        adSet.EndDate = request.EndDate;

        await _adSetRepository.UpdateAsync(adSet, cancellationToken);
        await InvalidateInsightsCacheAsync(tenantId, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "update adset", nameof(AdSet), adSet.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken);

        return Result<AdSetDto>.Ok(Map(adSet), "AdSet actualizado correctamente");
    }

    public Task<Result<AdSetDto>> PauseAdSetAsync(Guid adSetId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(adSetId, "PAUSED", cancellationToken);

    public Task<Result<AdSetDto>> ActivateAdSetAsync(Guid adSetId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(adSetId, "ACTIVE", cancellationToken);

    private async Task<Result<AdSetDto>> ChangeStatusAsync(Guid adSetId, string status, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdSetDto>.Fail("Tenant no resuelto");

        var adSet = await _adSetRepository.GetByIdAsync(tenantId, adSetId, cancellationToken);
        if (adSet is null)
            return Result<AdSetDto>.Fail("AdSet no encontrado");

        await _metaAdsService.UpdateAdSetStatusAsync(tenantId, new MetaAdSetStatusUpdateRequest(adSet.MetaAdSetId, status), cancellationToken);

        adSet.Status = status;
        await _adSetRepository.UpdateAsync(adSet, cancellationToken);
        await InvalidateInsightsCacheAsync(tenantId, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, status == "PAUSED" ? "pause adset" : "activate adset", nameof(AdSet), adSet.Id.ToString(), JsonSerializer.Serialize(new { adSet.Status }), cancellationToken);

        return Result<AdSetDto>.Ok(Map(adSet), $"Estado actualizado a {status}");
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

    private static AdSetDto Map(AdSet adSet) =>
        new(adSet.Id, adSet.MetaAdSetId, adSet.CampaignId, adSet.Name, adSet.Status, adSet.Budget, adSet.BillingEvent, adSet.OptimizationGoal, adSet.BidStrategy, adSet.TargetingJson, adSet.StartDate, adSet.EndDate);

}
