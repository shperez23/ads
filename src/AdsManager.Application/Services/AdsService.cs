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
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _auditService;

    public AdsService(IAdSetRepository adSetRepository, IAdRepository adRepository, IMetaAdsService metaAdsService, ITenantProvider tenantProvider, IAuditService auditService)
    {
        _adSetRepository = adSetRepository;
        _adRepository = adRepository;
        _metaAdsService = metaAdsService;
        _tenantProvider = tenantProvider;
        _auditService = auditService;
    }

    public async Task<Result<IReadOnlyCollection<AdDto>>> GetAdsAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<IReadOnlyCollection<AdDto>>.Fail("Tenant no resuelto");

        var ads = await _adRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<AdDto>>.Ok(ads.Select(Map).ToArray());
    }

    public async Task<Result<AdDto>> GetAdByIdAsync(Guid adId, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdDto>.Fail("Tenant no resuelto");

        var ad = await _adRepository.GetByIdAsync(tenantId, adId, cancellationToken);
        return ad is null
            ? Result<AdDto>.Fail("Ad no encontrado")
            : Result<AdDto>.Ok(Map(ad));
    }

    public Task<Result<AdDto>> CreateAdAsync(CreateAdRequest request, CancellationToken cancellationToken = default)
        => CreateAsync(request, cancellationToken);

    public async Task<Result<AdDto>> CreateAsync(CreateAdRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdDto>.Fail("Tenant no resuelto");

        var adSet = await _adSetRepository.GetByIdAsync(tenantId, request.AdSetId, cancellationToken);
        if (adSet is null)
            return Result<AdDto>.Fail("AdSet no encontrado");

        var metaAdId = await _metaAdsService.CreateAdAsync(tenantId, new MetaAdCreateRequest(request.Name, adSet.MetaAdSetId, request.Status, request.CreativeJson), cancellationToken);

        var ad = new Ad
        {
            TenantId = tenantId,
            AdSetId = adSet.Id,
            MetaAdId = metaAdId,
            Name = request.Name,
            Status = request.Status,
            CreativeJson = request.CreativeJson,
            PreviewUrl = request.PreviewUrl
        };

        await _adRepository.AddAsync(ad, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "create ad", nameof(Ad), ad.Id.ToString(), JsonSerializer.Serialize(ad), cancellationToken);

        return Result<AdDto>.Ok(Map(ad), "Ad creado correctamente");
    }

    public async Task<Result<AdDto>> UpdateAdAsync(Guid adId, UpdateAdRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdDto>.Fail("Tenant no resuelto");

        var ad = await _adRepository.GetByIdAsync(tenantId, adId, cancellationToken);
        if (ad is null)
            return Result<AdDto>.Fail("Ad no encontrado");

        await _metaAdsService.UpdateAdAsync(tenantId, new MetaAdUpdateRequest(ad.MetaAdId, request.Name, request.Status, request.CreativeJson), cancellationToken);

        ad.Name = request.Name;
        ad.Status = request.Status;
        ad.CreativeJson = request.CreativeJson;
        ad.PreviewUrl = request.PreviewUrl;

        await _adRepository.UpdateAsync(ad, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "update ad", nameof(Ad), ad.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken);

        return Result<AdDto>.Ok(Map(ad), "Ad actualizado correctamente");
    }

    public Task<Result<AdDto>> PauseAdAsync(Guid adId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(adId, "PAUSED", cancellationToken);

    public Task<Result<AdDto>> ActivateAdAsync(Guid adId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(adId, "ACTIVE", cancellationToken);

    private async Task<Result<AdDto>> ChangeStatusAsync(Guid adId, string status, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<AdDto>.Fail("Tenant no resuelto");

        var ad = await _adRepository.GetByIdAsync(tenantId, adId, cancellationToken);
        if (ad is null)
            return Result<AdDto>.Fail("Ad no encontrado");

        await _metaAdsService.UpdateAdStatusAsync(tenantId, new MetaAdStatusUpdateRequest(ad.MetaAdId, status), cancellationToken);

        ad.Status = status;
        await _adRepository.UpdateAsync(ad, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, status == "PAUSED" ? "pause ad" : "activate ad", nameof(Ad), ad.Id.ToString(), JsonSerializer.Serialize(new { ad.Status }), cancellationToken);

        return Result<AdDto>.Ok(Map(ad), $"Estado actualizado a {status}");
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        return tenantId != Guid.Empty;
    }

    private static AdDto Map(Ad ad)
        => new(ad.Id, ad.MetaAdId, ad.AdSetId, ad.Name, ad.Status, ad.CreativeJson, ad.PreviewUrl);

}
