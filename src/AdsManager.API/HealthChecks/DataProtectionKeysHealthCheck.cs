using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdsManager.API.HealthChecks;

public sealed class DataProtectionKeysHealthCheck : IHealthCheck
{
    private readonly IKeyManager? _keyManager;

    public DataProtectionKeysHealthCheck(IKeyManager? keyManager = null)
    {
        _keyManager = keyManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_keyManager is null)
        {
            return Task.FromResult(HealthCheckResult.Healthy("DataProtection no requiere key manager en este host."));
        }

        var keys = _keyManager.GetAllKeys();
        if (!keys.Any())
        {
            return Task.FromResult(HealthCheckResult.Degraded("No se encontraron DataProtection keys activas."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("DataProtection keys disponibles.", new Dictionary<string, object>
        {
            ["keys"] = keys.Count,
            ["hasRevokedKeys"] = keys.Any(k => k.IsRevoked)
        }));
    }
}
