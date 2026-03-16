using AdsManager.Infrastructure.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AdsManager.API.HealthChecks;

public sealed class JwtConfigurationHealthCheck : IHealthCheck
{
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly IWebHostEnvironment _environment;

    public JwtConfigurationHealthCheck(IOptions<JwtOptions> jwtOptions, IWebHostEnvironment environment)
    {
        _jwtOptions = jwtOptions;
        _environment = environment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var jwtOptions = _jwtOptions.Value;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
            errors.Add("Issuer no configurado");
        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
            errors.Add("Audience no configurado");

        var effectiveSecret = Environment.GetEnvironmentVariable("ADSMANAGER_JWT_SECRET") ?? jwtOptions.SecretKey;
        if (string.IsNullOrWhiteSpace(effectiveSecret) || effectiveSecret.Length < 32)
        {
            if (!_environment.IsDevelopment())
            {
                errors.Add("SecretKey inválido o menor a 32 caracteres");
            }
        }

        var result = errors.Count == 0
            ? HealthCheckResult.Healthy("Configuración mínima JWT válida.")
            : HealthCheckResult.Unhealthy("Configuración JWT incompleta.", data: new Dictionary<string, object> { ["errors"] = errors });

        return Task.FromResult(result);
    }
}
