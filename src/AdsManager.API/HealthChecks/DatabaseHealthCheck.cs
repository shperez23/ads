using AdsManager.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdsManager.API.HealthChecks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AdsManagerDbContext _dbContext;

    public DatabaseHealthCheck(AdsManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("PostgreSQL no respondió correctamente.");
            }

            return HealthCheckResult.Healthy("PostgreSQL disponible.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error validando conexión a PostgreSQL.", ex);
        }
    }
}
