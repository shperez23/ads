using AdsManager.Infrastructure.Background;
using Hangfire;

namespace AdsManager.API.Extensions;

public static class HangfireRecurringJobsExtensions
{
    public static IApplicationBuilder RegisterRecurringSyncJobs(this IApplicationBuilder app)
    {
        RecurringJob.AddOrUpdate<SyncCampaignsJob>(
            "sync-campaigns-6-hours",
            job => job.ExecuteAsync(null, null, default),
            "0 */6 * * *");

        RecurringJob.AddOrUpdate<SyncAdSetsJob>(
            "sync-adsets-6-hours",
            job => job.ExecuteAsync(null, null, default),
            "15 */6 * * *");

        RecurringJob.AddOrUpdate<SyncAdsJob>(
            "sync-ads-6-hours",
            job => job.ExecuteAsync(null, null, default),
            "30 */6 * * *");

        RecurringJob.AddOrUpdate<SyncInsightsJob>(
            "sync-insights-24-hours",
            job => job.ExecuteAsync(null, null, default),
            Cron.Daily);

        RecurringJob.AddOrUpdate<RefreshMetaTokenJob>(
            "refresh-meta-tokens-hourly",
            job => job.ExecuteAsync(7, default),
            "0 * * * *");

        RecurringJob.AddOrUpdate<RuleEvaluationJob>(
            "evaluate-rules-hourly",
            job => job.ExecuteAsync(default),
            "5 * * * *");

        RecurringJob.AddOrUpdate<CleanupApiLogsJob>(
            "cleanup-api-logs-daily",
            job => job.ExecuteAsync(default),
            "20 2 * * *");

        RecurringJob.AddOrUpdate<CleanupAuditLogsJob>(
            "cleanup-audit-logs-daily",
            job => job.ExecuteAsync(default),
            "30 2 * * *");

        RecurringJob.AddOrUpdate<CleanupRuleExecutionLogsJob>(
            "cleanup-rule-execution-logs-daily",
            job => job.ExecuteAsync(default),
            "40 2 * * *");

        RecurringJob.AddOrUpdate<CleanupSyncJobRunsJob>(
            "cleanup-sync-job-runs-daily",
            job => job.ExecuteAsync(default),
            "50 2 * * *");

        return app;
    }
}
