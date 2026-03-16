using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces;

public interface IJobExecutionGuard
{
    Task<JobExecutionLease> TryStartAsync(string jobName, Guid? tenantId, string? adAccountId, CancellationToken cancellationToken = default);
    Task CompleteAsync(JobExecutionLease lease, string finalStatus, string? error = null, CancellationToken cancellationToken = default);
}

public sealed record JobExecutionLease(SyncJobRun Run, string LogicalKey, bool Acquired);
