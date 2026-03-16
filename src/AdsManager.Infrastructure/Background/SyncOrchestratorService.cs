using System.Diagnostics;
using AdsManager.Application.Interfaces;
using AdsManager.Domain.Entities;
using AdsManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class SyncOrchestratorService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<SyncOrchestratorService> _logger;
    private readonly IObservabilityMetrics _observabilityMetrics;

    public SyncOrchestratorService(
        IApplicationDbContext dbContext,
        ILogger<SyncOrchestratorService> logger,
        IObservabilityMetrics observabilityMetrics)
    {
        _dbContext = dbContext;
        _logger = logger;
        _observabilityMetrics = observabilityMetrics;
    }

    public async Task ExecutePerAccountAsync(
        string jobName,
        Func<Guid, string, CancellationToken, Task> executePerAccount,
        Guid? tenantId = null,
        string? adAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var run = new SyncJobRun
        {
            JobName = jobName,
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        _dbContext.SyncJobRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "{JobName} started with filters TenantId={TenantId} AdAccountId={AdAccountId}",
                jobName,
                tenantId,
                adAccountId);

            var connectedTenantIdsQuery = _dbContext.MetaConnections.AsNoTracking()
                .Where(x => x.Status == ConnectionStatus.Connected);

            if (tenantId.HasValue)
            {
                connectedTenantIdsQuery = connectedTenantIdsQuery.Where(x => x.TenantId == tenantId.Value);
            }

            var connectedTenantIds = await connectedTenantIdsQuery
                .Select(x => x.TenantId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var accountsQuery = _dbContext.AdAccounts.AsNoTracking()
                .Where(x => connectedTenantIds.Contains(x.TenantId));

            if (tenantId.HasValue)
            {
                accountsQuery = accountsQuery.Where(x => x.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(adAccountId))
            {
                accountsQuery = accountsQuery.Where(x => x.MetaAccountId == adAccountId);
            }

            var accounts = await accountsQuery.ToListAsync(cancellationToken);

            foreach (var account in accounts)
            {
                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["JobName"] = jobName,
                    ["TenantId"] = account.TenantId,
                    ["AdAccountId"] = account.MetaAccountId
                });

                _logger.LogInformation("Executing sync for tenant/account");
                await executePerAccount(account.TenantId, account.MetaAccountId, cancellationToken);
                _logger.LogInformation("Sync finished for tenant/account");
            }

            run.Status = "Succeeded";
            _observabilityMetrics.RecordSyncDuration(jobName, stopwatch.Elapsed.TotalMilliseconds, run.Status);
            _logger.LogInformation("{JobName} finished successfully in {ElapsedMs} ms", jobName, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.Error = ex.Message;
            _observabilityMetrics.RecordSyncDuration(jobName, stopwatch.Elapsed.TotalMilliseconds, run.Status);
            _logger.LogError(ex, "{JobName} failed after {ElapsedMs} ms", jobName, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            run.FinishedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
