using AdsManager.Application.Interfaces.Meta;

namespace AdsManager.Infrastructure.Background;

public sealed class SyncAdSetsJob
{
    private readonly SyncOrchestratorService _syncOrchestratorService;
    private readonly IMetaAdsService _metaAdsService;

    public SyncAdSetsJob(SyncOrchestratorService syncOrchestratorService, IMetaAdsService metaAdsService)
    {
        _syncOrchestratorService = syncOrchestratorService;
        _metaAdsService = metaAdsService;
    }

    public Task ExecuteAsync(Guid? tenantId = null, string? adAccountId = null, CancellationToken cancellationToken = default)
    {
        return _syncOrchestratorService.ExecutePerAccountAsync(
            jobName: nameof(SyncAdSetsJob),
            executePerAccount: (tenant, account, ct) => _metaAdsService.SyncAdSetsAsync(tenant, account, ct),
            tenantId: tenantId,
            adAccountId: adAccountId,
            cancellationToken: cancellationToken);
    }
}
