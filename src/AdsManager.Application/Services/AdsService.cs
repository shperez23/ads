using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Ads;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using System.Text.Json;

namespace AdsManager.Application.Services;

public sealed class AdsService : IAdsService
{
    private readonly IAdSetRepository _adSetRepository;
    private readonly IAdRepository _adRepository;
    private readonly IMetaAdsService _metaAdsService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;

    public AdsService(IAdSetRepository adSetRepository, IAdRepository adRepository, IMetaAdsService metaAdsService, IApplicationDbContext dbContext, ITenantProvider tenantProvider)
    {
        _adSetRepository = adSetRepository;
        _adRepository = adRepository;
        _metaAdsService = metaAdsService;
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<AdDto>> CreateAsync(CreateAdRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result<AdDto>.Fail("Tenant no resuelto");

        var adSet = await _adSetRepository.GetByIdAsync(tenantId.Value, request.AdSetId, cancellationToken);
        if (adSet is null)
            return Result<AdDto>.Fail("AdSet no encontrado");

        var metaAdId = await _metaAdsService.CreateAdAsync(tenantId.Value, new MetaAdCreateRequest(request.Name, adSet.MetaAdSetId, request.Status, request.CreativeJson), cancellationToken);

        var ad = new Ad
        {
            TenantId = tenantId.Value,
            AdSetId = adSet.Id,
            MetaAdId = metaAdId,
            Name = request.Name,
            Status = request.Status,
            CreativeJson = request.CreativeJson,
            PreviewUrl = request.PreviewUrl
        };

        await _adRepository.AddAsync(ad, cancellationToken);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId.Value,
            UserId = _tenantProvider.GetUserId() ?? Guid.Empty,
            Action = "create ad",
            EntityName = nameof(Ad),
            EntityId = ad.Id.ToString(),
            PayloadJson = JsonSerializer.Serialize(ad)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AdDto>.Ok(new AdDto(ad.Id, ad.MetaAdId, ad.AdSetId, ad.Name, ad.Status, ad.CreativeJson, ad.PreviewUrl), "Ad creado correctamente");
    }
}
