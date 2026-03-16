using System.Diagnostics;
using AdsManager.Application.Interfaces;
using AdsManager.Domain.Entities;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class SyncCampaignsJob
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;
    private readonly ILogger<SyncCampaignsJob> _logger;
    private readonly IObservabilityMetrics _observabilityMetrics;

    public SyncCampaignsJob(IApplicationDbContext dbContext, IMetaAdsService metaAdsService, ILogger<SyncCampaignsJob> logger, IObservabilityMetrics observabilityMetrics)
    {
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
        _logger = logger;
        _observabilityMetrics = observabilityMetrics;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var run = new SyncJobRun
        {
            JobName = "SyncCampaignsJob",
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        _dbContext.SyncJobRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var connections = await _dbContext.MetaConnections.AsNoTracking().Where(x => x.Status == ConnectionStatus.Connected).ToListAsync(cancellationToken);

            foreach (var connection in connections)
            {
                var accounts = await _dbContext.AdAccounts.AsNoTracking().Where(x => x.TenantId == connection.TenantId).ToListAsync(cancellationToken);
                foreach (var account in accounts)
                {
                    await _metaAdsService.SyncCampaignsAsync(connection.TenantId, account.MetaAccountId, cancellationToken);
                    await _metaAdsService.SyncAdSetsAsync(connection.TenantId, account.MetaAccountId, cancellationToken);
                    await _metaAdsService.SyncAdsAsync(connection.TenantId, account.MetaAccountId, cancellationToken);
                    _logger.LogInformation("SyncCampaignsJob completed for tenant {TenantId} adAccount {AdAccountId}", connection.TenantId, account.MetaAccountId);
                }
            }

            run.Status = "Succeeded";
            _observabilityMetrics.RecordSyncDuration(run.JobName, stopwatch.Elapsed.TotalMilliseconds, run.Status);
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.Error = ex.Message;
            _observabilityMetrics.RecordSyncDuration(run.JobName, stopwatch.Elapsed.TotalMilliseconds, "Failed");
            _logger.LogError(ex, "SyncCampaignsJob failed");
            throw;
        }
        finally
        {
            run.FinishedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
