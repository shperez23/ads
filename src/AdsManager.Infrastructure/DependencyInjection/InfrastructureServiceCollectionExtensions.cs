using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Infrastructure.Background;
using AdsManager.Infrastructure.Integrations.Meta;
using AdsManager.Infrastructure.Observability;
using AdsManager.Infrastructure.Persistence;
using AdsManager.Infrastructure.Persistence.Repositories;
using AdsManager.Infrastructure.Security;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdsManager.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = Environment.GetEnvironmentVariable("ADSMANAGER_DB_CONNECTION")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured");

        services.AddDbContext<AdsManagerDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<AdsManagerDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddDataProtection();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ISecretEncryptionService, SecretEncryptionService>();
        services.AddSingleton<IObservabilityMetrics, ObservabilityMetrics>();

        services.AddHttpClient<IMetaAdsService, MetaAdsService>();
        services.AddHttpClient<IMetaConnectionApiClient, MetaConnectionApiClient>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IAdAccountRepository, AdAccountRepository>();
        services.AddScoped<IAdSetRepository, AdSetRepository>();
        services.AddScoped<IAdRepository, AdRepository>();
        services.AddScoped<IInsightRepository, InsightRepository>();
        services.AddScoped<IMetaConnectionRepository, MetaConnectionRepository>();

        services.AddScoped<SyncOrchestratorService>();
        services.AddScoped<SyncCampaignsJob>();
        services.AddScoped<SyncAdSetsJob>();
        services.AddScoped<SyncAdsJob>();
        services.AddScoped<SyncInsightsJob>();
        services.AddScoped<RefreshMetaTokenJob>();

        services.AddHangfire(config => config
         .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
         .UseSimpleAssemblyNameTypeSerializer()
         .UseRecommendedSerializerSettings()
         .UsePostgreSqlStorage(options =>
         {
             options.UseNpgsqlConnection(connectionString);
         }));

        services.AddHangfireServer();

        return services;
    }
}
