using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdAccounts;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace AdsManager.Application.Services;

public sealed class AdAccountService : IAdAccountService
{
    private readonly IAdAccountRepository _adAccountRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _auditService;

    public AdAccountService(IAdAccountRepository adAccountRepository, IApplicationDbContext dbContext, IMetaAdsService metaAdsService, ITenantProvider tenantProvider, IAuditService auditService)
    {
        _adAccountRepository = adAccountRepository;
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
        _tenantProvider = tenantProvider;
        _auditService = auditService;
    }

    public async Task<Result<PagedResponse<AdAccountDto>>> GetAllAsync(AdAccountListRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<PagedResponse<AdAccountDto>>.Fail("Tenant no resuelto");

        var (items, total) = await _adAccountRepository.GetPagedByTenantAsync(tenantId, request, cancellationToken);
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Result<PagedResponse<AdAccountDto>>.Ok(new PagedResponse<AdAccountDto>(items.Select(Map).ToArray(), page, pageSize, total, totalPages));
    }

    public async Task<Result<IReadOnlyCollection<AdAccountDto>>> ImportFromMetaAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<IReadOnlyCollection<AdAccountDto>>.Fail("Tenant no resuelto");

        try
        {
            var metaAccounts = await _metaAdsService.GetAdAccountsAsync(tenantId, cancellationToken);
            var existingAccounts = await _adAccountRepository.GetByTenantAsync(tenantId, cancellationToken);
            var existingByMetaId = existingAccounts.ToDictionary(x => x.MetaAccountId, StringComparer.OrdinalIgnoreCase);

            foreach (var metaAccount in metaAccounts)
            {
                if (!existingByMetaId.TryGetValue(metaAccount.Id, out var existing))
                {
                    _dbContext.AdAccounts.Add(new AdAccount
                    {
                        TenantId = tenantId,
                        MetaAccountId = metaAccount.Id,
                        Name = metaAccount.Name,
                        Currency = metaAccount.Currency,
                        TimezoneName = metaAccount.TimezoneName,
                        Status = metaAccount.AccountStatus
                    });
                    continue;
                }

                existing.Name = metaAccount.Name;
                existing.Currency = metaAccount.Currency;
                existing.TimezoneName = metaAccount.TimezoneName;
                existing.Status = metaAccount.AccountStatus;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "sync adaccounts", nameof(AdAccount), string.Empty, JsonSerializer.Serialize(metaAccounts), cancellationToken);

            var updatedAccounts = await _adAccountRepository.GetByTenantAsync(tenantId, cancellationToken);
            return Result<IReadOnlyCollection<AdAccountDto>>.Ok(updatedAccounts.Select(Map).ToArray(), "AdAccounts importadas correctamente");
        }
        catch (InvalidOperationException ex)
        {
            return Result<IReadOnlyCollection<AdAccountDto>>.Fail("No se pudo importar las cuentas publicitarias desde Meta.", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return Result<IReadOnlyCollection<AdAccountDto>>.Fail(BuildMetaRequestFailureMessage(ex, "importar las cuentas publicitarias desde Meta"), ex.Message);
        }
    }

    public async Task<Result<string>> SyncAsync(Guid adAccountId, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<string>.Fail("Tenant no resuelto");

        var adAccount = await _adAccountRepository.GetByIdAsync(tenantId, adAccountId, cancellationToken);
        if (adAccount is null)
            return Result<string>.Fail("Ad account no encontrada");

        try
        {
            await _metaAdsService.SyncCampaignsAsync(tenantId, adAccount.MetaAccountId, cancellationToken);
            await _metaAdsService.SyncAdSetsAsync(tenantId, adAccount.MetaAccountId, cancellationToken);
            await _metaAdsService.SyncAdsAsync(tenantId, adAccount.MetaAccountId, cancellationToken);

            await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "sync adaccount", nameof(AdAccount), adAccount.Id.ToString(), JsonSerializer.Serialize(new { adAccount.MetaAccountId }), cancellationToken);

            return Result<string>.Ok(adAccount.Id.ToString(), "Sincronización de AdAccount completada");
        }
        catch (InvalidOperationException ex)
        {
            return Result<string>.Fail("No se pudo sincronizar la cuenta publicitaria desde Meta.", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return Result<string>.Fail(BuildMetaRequestFailureMessage(ex, "sincronizar la cuenta publicitaria desde Meta"), ex.Message);
        }
    }


    private static string BuildMetaRequestFailureMessage(HttpRequestException exception, string operation)
        => exception.StatusCode switch
        {
            HttpStatusCode.BadRequest => $"Meta rechazó la solicitud al intentar {operation}.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => $"La conexión de Meta no tiene permisos válidos para {operation}.",
            HttpStatusCode.TooManyRequests => $"Meta limitó temporalmente la operación al intentar {operation}.",
            HttpStatusCode.RequestTimeout => $"Meta tardó demasiado en responder al intentar {operation}.",
            _ => $"Ocurrió un error al intentar {operation}."
        };

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        return tenantId != Guid.Empty;
    }

    private static AdAccountDto Map(AdAccount adAccount)
        => new(adAccount.Id, adAccount.MetaAccountId, adAccount.Name, adAccount.Currency, adAccount.TimezoneName, adAccount.Status);
}
