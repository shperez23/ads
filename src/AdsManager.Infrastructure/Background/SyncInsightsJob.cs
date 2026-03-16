using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Services;

namespace AdsManager.Infrastructure.Background;

public sealed class SyncInsightsJob
{
    private readonly SyncOrchestratorService _syncOrchestratorService;
    private readonly IMetaAdsService _metaAdsService;
    private readonly ICacheService _cacheService;

    public SyncInsightsJob(SyncOrchestratorService syncOrchestratorService, IMetaAdsService metaAdsService, ICacheService cacheService)
    {
        _syncOrchestratorService = syncOrchestratorService;
        _metaAdsService = metaAdsService;
        _cacheService = cacheService;
    }

    public Task ExecuteAsync(Guid? tenantId = null, string? adAccountId = null, CancellationToken cancellationToken = default)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        return _syncOrchestratorService.ExecutePerAccountAsync(
            jobName: nameof(SyncInsightsJob),
            executePerAccount: async (tenant, account, ct) =>
            {
                await _metaAdsService.SyncInsightsAsync(tenant, account, yesterday, yesterday, ct);
                foreach (var prefix in InsightsCacheKeys.TenantPrefixes(tenant))
                    await _cacheService.RemoveByPrefixAsync(prefix, ct);
            },
            tenantId: tenantId,
            adAccountId: adAccountId,
            cancellationToken: cancellationToken);
    }
}
