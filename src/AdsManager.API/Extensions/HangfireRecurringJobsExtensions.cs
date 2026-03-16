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

        return app;
    }
}
