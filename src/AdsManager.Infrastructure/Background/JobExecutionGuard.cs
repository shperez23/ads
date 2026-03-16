using AdsManager.Application.Interfaces;
using AdsManager.Domain.Entities;
using AdsManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class JobExecutionGuard : IJobExecutionGuard
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<JobExecutionGuard> _logger;

    public JobExecutionGuard(IApplicationDbContext dbContext, ILogger<JobExecutionGuard> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<JobExecutionLease> TryStartAsync(string jobName, Guid? tenantId, string? adAccountId, CancellationToken cancellationToken = default)
    {
        var logicalKey = BuildLogicalKey(jobName, tenantId, adAccountId);
        var run = new SyncJobRun
        {
            JobName = jobName,
            TenantId = tenantId,
            AdAccountId = NormalizeAdAccount(adAccountId),
            LogicalKey = logicalKey,
            StartedAt = DateTime.UtcNow,
            Status = SyncJobRunStatus.Queued
        };

        _dbContext.SyncJobRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var runningExists = await _dbContext.SyncJobRuns.AsNoTracking().AnyAsync(
            x => x.Id != run.Id && x.LogicalKey == logicalKey && x.Status == SyncJobRunStatus.Running,
            cancellationToken);

        if (runningExists)
        {
            run.Status = SyncJobRunStatus.Skipped;
            run.FinishedAt = DateTime.UtcNow;
            run.Error = "Skipped due to active execution with same logical key.";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new JobExecutionLease(run, logicalKey, false);
        }

        run.Status = SyncJobRunStatus.Running;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new JobExecutionLease(run, logicalKey, true);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogInformation(ex, "Job execution skipped due to concurrency lock for {LogicalKey}", logicalKey);
            run.Status = SyncJobRunStatus.Skipped;
            run.FinishedAt = DateTime.UtcNow;
            run.Error = "Skipped due to active execution with same logical key.";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new JobExecutionLease(run, logicalKey, false);
        }
    }

    public async Task CompleteAsync(JobExecutionLease lease, string finalStatus, string? error = null, CancellationToken cancellationToken = default)
    {
        lease.Run.Status = finalStatus;
        lease.Run.Error = error;
        lease.Run.FinishedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildLogicalKey(string jobName, Guid? tenantId, string? adAccountId)
    {
        var tenant = tenantId?.ToString("N") ?? "all-tenants";
        var account = string.IsNullOrWhiteSpace(adAccountId) ? "all-accounts" : adAccountId.Trim();
        return $"{jobName}:{tenant}:{account}";
    }

    private static string? NormalizeAdAccount(string? adAccountId)
    {
        return string.IsNullOrWhiteSpace(adAccountId) ? null : adAccountId.Trim();
    }
}
