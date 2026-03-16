using AdsManager.Application.Interfaces.Meta;

namespace AdsManager.Infrastructure.Background;

public sealed class SyncCampaignsJob
{
    private readonly SyncOrchestratorService _syncOrchestratorService;
    private readonly IMetaAdsService _metaAdsService;

    public SyncCampaignsJob(SyncOrchestratorService syncOrchestratorService, IMetaAdsService metaAdsService)
    {
        _syncOrchestratorService = syncOrchestratorService;
        _metaAdsService = metaAdsService;
    }

    public Task ExecuteAsync(Guid? tenantId = null, string? adAccountId = null, CancellationToken cancellationToken = default)
    {
        return _syncOrchestratorService.ExecutePerAccountAsync(
            jobName: nameof(SyncCampaignsJob),
            executePerAccount: (tenant, account, ct) => _metaAdsService.SyncCampaignsAsync(tenant, account, ct),
            tenantId: tenantId,
            adAccountId: adAccountId,
            cancellationToken: cancellationToken);
    }
}
