using AdsManager.Application.Interfaces;
using AdsManager.Domain.Entities;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class SyncInsightsJob
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;
    private readonly ILogger<SyncInsightsJob> _logger;

    public SyncInsightsJob(IApplicationDbContext dbContext, IMetaAdsService metaAdsService, ILogger<SyncInsightsJob> logger)
    {
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var run = new SyncJobRun
        {
            JobName = "SyncInsightsJob",
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        _dbContext.SyncJobRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var connections = await _dbContext.MetaConnections.AsNoTracking().Where(x => x.Status == ConnectionStatus.Connected).ToListAsync(cancellationToken);

            foreach (var connection in connections)
            {
                var accounts = await _dbContext.AdAccounts.AsNoTracking().Where(x => x.TenantId == connection.TenantId).ToListAsync(cancellationToken);
                foreach (var account in accounts)
                {
                    await _metaAdsService.SyncInsightsAsync(connection.TenantId, account.MetaAccountId, yesterday, yesterday, cancellationToken);
                    _logger.LogInformation("SyncInsightsJob completed for tenant {TenantId} adAccount {AdAccountId}", connection.TenantId, account.MetaAccountId);
                }
            }

            run.Status = "Succeeded";
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.Error = ex.Message;
            _logger.LogError(ex, "SyncInsightsJob failed");
            throw;
        }
        finally
        {
            run.FinishedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
