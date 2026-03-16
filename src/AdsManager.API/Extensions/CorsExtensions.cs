using AdsManager.Application.Configuration;

namespace AdsManager.API.Extensions;

public static class CorsExtensions
{
    private const string CorsPolicyName = "ConfiguredCorsPolicy";

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        var allowedOrigins = Normalize(options.AllowedOrigins);
        var allowedMethods = Normalize(options.AllowedMethods);
        var allowedHeaders = Normalize(options.AllowedHeaders);

        services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy(CorsPolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else if (options.AllowCredentials)
                {
                    throw new InvalidOperationException("Cors:AllowedOrigins must be configured when Cors:AllowCredentials is true.");
                }
                else
                {
                    policy.AllowAnyOrigin();
                }

                if (allowedMethods.Length > 0)
                {
                    policy.WithMethods(allowedMethods);
                }
                else
                {
                    policy.AllowAnyMethod();
                }

                if (allowedHeaders.Length > 0)
                {
                    policy.WithHeaders(allowedHeaders);
                }
                else
                {
                    policy.AllowAnyHeader();
                }

                if (options.AllowCredentials)
                {
                    policy.AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseConfiguredCors(this WebApplication app)
    {
        app.UseCors(CorsPolicyName);
        return app;
    }

    private static string[] Normalize(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];
}
