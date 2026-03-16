using System.Text.Json;
using AdsManager.API.HealthChecks;
using AdsManager.Application.Configuration;
using AdsManager.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AdsManager.API.Extensions;

public static class HealthChecksExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static IServiceCollection AddConfiguredHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<HangfireHealthCheck>("hangfire", tags: ["ready"])
            .AddCheck<MetaConnectionHealthSummaryCheck>("meta-connections-summary", tags: ["ready"])
            .AddCheck("jwt-configuration", sp =>
            {
                var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
                var environment = sp.GetRequiredService<IWebHostEnvironment>();
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
                    errors.Add("Issuer no configurado");
                if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
                    errors.Add("Audience no configurado");

                var effectiveSecret = Environment.GetEnvironmentVariable("ADSMANAGER_JWT_SECRET") ?? jwtOptions.SecretKey;
                if (string.IsNullOrWhiteSpace(effectiveSecret) || effectiveSecret.Length < 32)
                {
                    if (!environment.IsDevelopment())
                    {
                        errors.Add("SecretKey inválido o menor a 32 caracteres");
                    }
                }

                return errors.Count == 0
                    ? HealthCheckResult.Healthy("Configuración mínima JWT válida.")
                    : HealthCheckResult.Unhealthy("Configuración JWT incompleta.", data: new Dictionary<string, object> { ["errors"] = errors });
            }, tags: ["ready"])
            .AddCheck("data-protection-keys", sp =>
            {
                var keyManager = sp.GetService<IKeyManager>();
                if (keyManager is null)
                {
                    return HealthCheckResult.Healthy("DataProtection no requiere key manager en este host.");
                }

                var keys = keyManager.GetAllKeys();
                if (!keys.Any())
                {
                    return HealthCheckResult.Degraded("No se encontraron DataProtection keys activas.");
                }

                return HealthCheckResult.Healthy("DataProtection keys disponibles.", new Dictionary<string, object>
                {
                    ["keys"] = keys.Count,
                    ["hasRevokedKeys"] = keys.Any(k => k.IsRevoked)
                });
            }, tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapConfiguredHealthChecks(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
    {
        var options = configuration.GetSection(FeatureExposureOptions.SectionName).Get<FeatureExposureOptions>() ?? new FeatureExposureOptions();

        endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponseAsync
        }).AllowAnonymous();

        var readyEndpoint = endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteResponseAsync
        });

        if (options.ReadyHealthRequiresAuth)
        {
            readyEndpoint.RequireAuthorization();
        }
        else
        {
            readyEndpoint.AllowAnonymous();
        }

        return endpoints;
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
