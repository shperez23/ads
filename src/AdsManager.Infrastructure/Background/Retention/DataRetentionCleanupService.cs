using System.Text.Json;
using AdsManager.Application.Configuration;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdsManager.Infrastructure.Background.Retention;

public sealed class DataRetentionCleanupService : IDataRetentionCleanupService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOptions<DataRetentionOptions> _options;
    private readonly ILogger<DataRetentionCleanupService> _logger;

    public DataRetentionCleanupService(
        IApplicationDbContext dbContext,
        IOptions<DataRetentionOptions> options,
        ILogger<DataRetentionCleanupService> logger)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
    }

    public Task<int> CleanupApiLogsAsync(CancellationToken cancellationToken = default)
        => CleanupAsync(
            tableName: nameof(ApiLog),
            retentionDays: _options.Value.ApiLogsDays,
            deleteAction: cutoffUtc => _dbContext.ApiLogs
                .Where(x => x.CreatedAt < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken),
            cancellationToken: cancellationToken);

    public Task<int> CleanupAuditLogsAsync(CancellationToken cancellationToken = default)
        => CleanupAsync(
            tableName: nameof(AuditLog),
            retentionDays: _options.Value.AuditLogsDays,
            deleteAction: cutoffUtc => _dbContext.AuditLogs
                .Where(x => x.CreatedAt < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken),
            cancellationToken: cancellationToken);

    public Task<int> CleanupRuleExecutionLogsAsync(CancellationToken cancellationToken = default)
        => CleanupAsync(
            tableName: nameof(RuleExecutionLog),
            retentionDays: _options.Value.RuleExecutionLogsDays,
            deleteAction: cutoffUtc => _dbContext.RuleExecutionLogs
                .Where(x => x.ExecutedAt < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken),
            cancellationToken: cancellationToken);

    public Task<int> CleanupSyncJobRunsAsync(CancellationToken cancellationToken = default)
        => CleanupAsync(
            tableName: nameof(SyncJobRun),
            retentionDays: _options.Value.SyncJobRunsDays,
            deleteAction: cutoffUtc => _dbContext.SyncJobRuns
                .Where(x => x.StartedAt < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken),
            cancellationToken: cancellationToken);

    private async Task<int> CleanupAsync(
        string tableName,
        int retentionDays,
        Func<DateTime, Task<int>> deleteAction,
        CancellationToken cancellationToken)
    {
        if (retentionDays <= 0)
        {
            _logger.LogInformation("Cleanup skipped for {TableName} because retention days is {RetentionDays}", tableName, retentionDays);
            return 0;
        }

        var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedRows = await deleteAction(cutoffUtc);

        _logger.LogInformation(
            "Cleanup completed for {TableName}. RetentionDays={RetentionDays} CutoffUtc={CutoffUtc} DeletedRows={DeletedRows}",
            tableName,
            retentionDays,
            cutoffUtc,
            deletedRows);

        await RegisterCleanupAuditAsync(tableName, retentionDays, cutoffUtc, deletedRows, cancellationToken);
        return deletedRows;
    }

    private async Task RegisterCleanupAuditAsync(string tableName, int retentionDays, DateTime cutoffUtc, int deletedRows, CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = Guid.Empty,
            UserId = Guid.Empty,
            Action = "retention cleanup",
            EntityName = tableName,
            EntityId = tableName,
            PayloadJson = JsonSerializer.Serialize(new
            {
                retentionDays,
                cutoffUtc,
                deletedRows
            }),
            TraceId = "hangfire-retention-cleanup"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
