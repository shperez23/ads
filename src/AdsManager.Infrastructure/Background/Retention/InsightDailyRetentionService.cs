using System.Text.Json;
using AdsManager.Application.Configuration;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdsManager.Infrastructure.Background.Retention;

public sealed class InsightDailyRetentionService : IInsightDailyRetentionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOptions<DataRetentionOptions> _options;
    private readonly ILogger<InsightDailyRetentionService> _logger;

    public InsightDailyRetentionService(
        IApplicationDbContext dbContext,
        IOptions<DataRetentionOptions> options,
        ILogger<InsightDailyRetentionService> logger)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
    }

    public async Task ApplyConfiguredPolicyAsync(CancellationToken cancellationToken = default)
    {
        var settings = _options.Value.InsightDaily;
        if (!settings.Enabled)
        {
            _logger.LogInformation("InsightDaily retention is disabled. No purge or archive action will run.");
            return;
        }

        if (!string.Equals(settings.Mode, "Purge", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("InsightDaily retention mode '{Mode}' is configured. Current implementation only executes purge mode.", settings.Mode);
            return;
        }

        var totalDeleted = 0;

        if (settings.GlobalRetentionDays.HasValue && settings.GlobalRetentionDays.Value > 0)
        {
            var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-settings.GlobalRetentionDays.Value));
            var deleted = await _dbContext.InsightsDaily
                .Where(x => x.Date < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);
            totalDeleted += deleted;
        }

        foreach (var policy in settings.TenantPolicies)
        {
            var cutoffDate = ResolveCutoffDate(policy);
            if (!cutoffDate.HasValue)
                continue;

            var deleted = await _dbContext.InsightsDaily
                .Where(x => x.TenantId == policy.TenantId && x.Date < cutoffDate.Value)
                .ExecuteDeleteAsync(cancellationToken);

            totalDeleted += deleted;
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = Guid.Empty,
            UserId = Guid.Empty,
            Action = "retention cleanup",
            EntityName = nameof(InsightDaily),
            EntityId = nameof(InsightDaily),
            PayloadJson = JsonSerializer.Serialize(new
            {
                settings.Mode,
                settings.GlobalRetentionDays,
                tenantPolicies = settings.TenantPolicies.Count,
                totalDeleted
            }),
            TraceId = "hangfire-retention-cleanup"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("InsightDaily retention executed in mode {Mode}. DeletedRows={DeletedRows}", settings.Mode, totalDeleted);
    }

    private static DateOnly? ResolveCutoffDate(InsightDailyTenantRetentionPolicy policy)
    {
        if (policy.PurgeBeforeDate.HasValue)
            return policy.PurgeBeforeDate.Value;

        if (policy.RetentionDays.HasValue && policy.RetentionDays.Value > 0)
            return DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-policy.RetentionDays.Value));

        return null;
    }
}
