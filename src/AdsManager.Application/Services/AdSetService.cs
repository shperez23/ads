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

    public AdSetService(ICampaignRepository campaignRepository, IAdSetRepository adSetRepository, IApplicationDbContext dbContext, IMetaAdsService metaAdsService, ITenantProvider tenantProvider)
    {
        _campaignRepository = campaignRepository;
        _adSetRepository = adSetRepository;
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<AdSetDto>> CreateAsync(CreateAdSetRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result<AdSetDto>.Fail("Tenant no resuelto");

        var campaign = await _campaignRepository.GetByIdAsync(tenantId.Value, request.CampaignId, cancellationToken);
        if (campaign is null)
            return Result<AdSetDto>.Fail("Campaña no encontrada");

        var adAccount = await _dbContext.AdAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == campaign.AdAccountId, cancellationToken);

        if (adAccount is null)
            return Result<AdSetDto>.Fail("Ad account no encontrada");

        var metaAdSetId = await _metaAdsService.CreateAdSetAsync(tenantId.Value, adAccount.MetaAccountId,
            new MetaAdSetCreateRequest(request.Name, campaign.MetaCampaignId, request.Status, request.DailyBudget, request.BillingEvent, request.OptimizationGoal, request.TargetingJson),
            cancellationToken);

        var adSet = new AdSet
        {
            TenantId = tenantId.Value,
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
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId.Value,
            UserId = _tenantProvider.GetUserId() ?? Guid.Empty,
            Action = "create adset",
            EntityName = nameof(AdSet),
            EntityId = adSet.Id.ToString(),
            PayloadJson = JsonSerializer.Serialize(adSet)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AdSetDto>.Ok(Map(adSet), "AdSet creado correctamente");
    }

    private static AdSetDto Map(AdSet adSet) =>
        new(adSet.Id, adSet.MetaAdSetId, adSet.CampaignId, adSet.Name, adSet.Status, adSet.Budget, adSet.BillingEvent, adSet.OptimizationGoal, adSet.BidStrategy, adSet.TargetingJson, adSet.StartDate, adSet.EndDate);
}
