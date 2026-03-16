namespace AdsManager.Application.Interfaces.Services;

public interface IDataRetentionCleanupService
{
    Task<int> CleanupApiLogsAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupAuditLogsAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupRuleExecutionLogsAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupSyncJobRunsAsync(CancellationToken cancellationToken = default);
}
