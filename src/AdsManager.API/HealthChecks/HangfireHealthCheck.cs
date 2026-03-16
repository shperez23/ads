using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdsManager.API.HealthChecks;

public sealed class HangfireHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            _ = monitoringApi.Queues();

            return Task.FromResult(HealthCheckResult.Healthy("Hangfire storage disponible."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Error validando Hangfire storage.", ex));
        }
    }
}
