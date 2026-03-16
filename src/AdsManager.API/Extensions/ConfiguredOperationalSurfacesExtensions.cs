using System.Net;
using System.Security.Claims;
using AdsManager.API.Middleware;
using AdsManager.Application.Configuration;
using Hangfire;
using Asp.Versioning.ApiExplorer;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;

namespace AdsManager.API.Extensions;

public static class ConfiguredOperationalSurfacesExtensions
{
    public static IApplicationBuilder UseConfiguredSwagger(this WebApplication app)
    {
        var options = app.Configuration.GetSection(FeatureExposureOptions.SectionName).Get<FeatureExposureOptions>() ?? new FeatureExposureOptions();
        var isNonProductionEnvironment = app.Environment.IsDevelopment() || app.Environment.IsStaging();
        var shouldEnableSwagger = isNonProductionEnvironment || options.SwaggerEnabled;

        if (!shouldEnableSwagger)
        {
            return app;
        }

        if (app.Environment.IsProduction())
        {
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase), branch =>
            {
                branch.UseAuthentication();
                branch.UseAuthorization();
                branch.Use(async (context, next) =>
                {
                    var isAdmin = context.User.Identity?.IsAuthenticated == true
                        && context.User.Claims.Any(claim =>
                            claim.Type == ClaimTypes.Role
                            && string.Equals(claim.Value, "Admin", StringComparison.OrdinalIgnoreCase));

                    if (!isAdmin)
                    {
                        await context.ChallengeAsync();
                        return;
                    }

                    await next();
                });
            });
        }

        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"AdsManager API {description.GroupName.ToUpperInvariant()}");
            }
        });

        return app;
    }

    public static IApplicationBuilder UseConfiguredHangfireDashboard(this WebApplication app, string path = "/hangfire")
    {
        var options = app.Configuration.GetSection(FeatureExposureOptions.SectionName).Get<FeatureExposureOptions>() ?? new FeatureExposureOptions();
        var isNonProductionEnvironment = app.Environment.IsDevelopment() || app.Environment.IsStaging();
        var shouldEnableDashboard = isNonProductionEnvironment || options.HangfireDashboardEnabled;

        if (!shouldEnableDashboard)
        {
            return app;
        }

        var allowlistedIps = options.HangfireDashboardIpAllowlist
            .Select(ParseIpAddress)
            .OfType<IPAddress>()
            .ToArray();

        app.UseHangfireDashboard(path, new DashboardOptions
        {
            Authorization = [new HangfireAdminAuthorizationFilter(allowlistedIps)]
        });

        return app;
    }

    private static IPAddress? ParseIpAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return IPAddress.TryParse(value, out var parsed)
            ? parsed
            : null;
    }
}
