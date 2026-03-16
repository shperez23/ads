using AdsManager.Application.Interfaces;
using AdsManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdsManager.API.HealthChecks;

public sealed class MetaConnectionHealthSummaryCheck : IHealthCheck
{
    private readonly IApplicationDbContext _dbContext;

    public MetaConnectionHealthSummaryCheck(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var total = await _dbContext.MetaConnections.AsNoTracking().CountAsync(cancellationToken);
            var connected = await _dbContext.MetaConnections.AsNoTracking()
                .CountAsync(x => x.Status == ConnectionStatus.Connected, cancellationToken);
            var expiringSoon = await _dbContext.MetaConnections.AsNoTracking()
                .CountAsync(x => x.TokenExpiration <= utcNow.AddHours(24), cancellationToken);
            var unhealthy = await _dbContext.MetaConnections.AsNoTracking()
                .CountAsync(x => x.Status != ConnectionStatus.Connected, cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["total"] = total,
                ["connected"] = connected,
                ["unhealthy"] = unhealthy,
                ["expiringSoon"] = expiringSoon
            };

            if (total == 0)
            {
                return HealthCheckResult.Degraded("No hay conexiones Meta registradas.", data: data);
            }

            if (unhealthy > 0)
            {
                return HealthCheckResult.Degraded("Existen conexiones Meta con estado no conectado.", data: data);
            }

            return HealthCheckResult.Healthy("Resumen de conexiones Meta consistente.", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error construyendo resumen local de conexiones Meta.", ex);
        }
    }
}
