using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdSets;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Application.Services;

public sealed class AdSetService : IAdSetService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IAdSetRepository _adSetRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;

    public AdSetService(ICampaignRepository campaignRepository, IAdSetRepository adSetRepository, IApplicationDbContext dbContext, IMetaAdsService metaAdsService)
    {
        _campaignRepository = campaignRepository;
        _adSetRepository = adSetRepository;
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
    }

    public async Task<Result<AdSetDto>> CreateAsync(Guid tenantId, CreateAdSetRequest request, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(tenantId, request.CampaignId, cancellationToken);
        if (campaign is null)
            return Result<AdSetDto>.Fail("Campaña no encontrada");

        var adAccount = await _dbContext.AdAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == campaign.AdAccountId, cancellationToken);

        if (adAccount is null)
            return Result<AdSetDto>.Fail("Ad account no encontrada");

        var metaAdSetId = await _metaAdsService.CreateAdSetAsync(adAccount.MetaAccountId,
            new MetaAdSetCreateRequest(request.Name, campaign.MetaCampaignId, request.Status, request.DailyBudget, request.BillingEvent, request.OptimizationGoal, request.TargetingJson),
            request.AccessToken,
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

        return Result<AdSetDto>.Ok(Map(adSet), "AdSet creado correctamente");
    }

    private static AdSetDto Map(AdSet adSet) =>
        new(adSet.Id, adSet.MetaAdSetId, adSet.CampaignId, adSet.Name, adSet.Status, adSet.Budget, adSet.BillingEvent, adSet.OptimizationGoal, adSet.BidStrategy, adSet.TargetingJson, adSet.StartDate, adSet.EndDate);
}
