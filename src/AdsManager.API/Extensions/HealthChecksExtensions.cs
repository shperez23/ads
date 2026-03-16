using System.Text.Json;
using AdsManager.API.HealthChecks;
using AdsManager.Application.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
            .AddCheck<JwtConfigurationHealthCheck>("jwt-configuration", tags: ["ready"])
            .AddCheck<DataProtectionKeysHealthCheck>("data-protection-keys", tags: ["ready"]);

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
