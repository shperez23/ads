using AdsManager.Application.Configuration;
using OpenTelemetry.Metrics;

namespace AdsManager.API.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddConfiguredObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.SectionName));

        var options = configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>() ?? new ObservabilityOptions();
        if (!options.EnablePrometheus)
        {
            return services;
        }

        services.AddOpenTelemetry()
            .WithMetrics(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("AdsManager.Observability")
                .AddPrometheusExporter());

        return services;
    }

    public static IApplicationBuilder UseConfiguredMetricsEndpoint(this WebApplication app)
    {
        var options = app.Configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>() ?? new ObservabilityOptions();
        if (!options.EnablePrometheus)
        {
            return app;
        }

        app.MapPrometheusScrapingEndpoint(options.MetricsEndpoint);
        return app;
    }
}
