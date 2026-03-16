using AdsManager.Application.Configuration;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Application.Services;
using AdsManager.Infrastructure.Background;
using AdsManager.Infrastructure.Caching;
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
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace AdsManager.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<AuthProtectionOptions>(configuration.GetSection(AuthProtectionOptions.SectionName));

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
        services.AddScoped<IAuthProtectionService, AuthProtectionService>();

        RegisterCacheServices(services, configuration, environment);

        services.AddSingleton<IObservabilityMetrics, ObservabilityMetrics>();

        services.AddHttpClient<IMetaAdsService, MetaAdsService>();
        services.AddHttpClient<IMetaConnectionApiClient, MetaConnectionApiClient>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IAdAccountRepository, AdAccountRepository>();
        services.AddScoped<IAdSetRepository, AdSetRepository>();
        services.AddScoped<IAdRepository, AdRepository>();
        services.AddScoped<IInsightRepository, InsightRepository>();
        services.AddScoped<IMetaConnectionRepository, MetaConnectionRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();

        services.AddScoped<IJobExecutionGuard, JobExecutionGuard>();
        services.AddScoped<SyncOrchestratorService>();
        services.AddScoped<SyncCampaignsJob>();
        services.AddScoped<SyncAdSetsJob>();
        services.AddScoped<SyncAdsJob>();
        services.AddScoped<SyncInsightsJob>();
        services.AddScoped<RefreshMetaTokenJob>();
        services.AddScoped<RuleEvaluationJob>();

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

    private static void RegisterCacheServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        var provider = cacheOptions.Provider?.Trim();
        var useRedis = string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase);
        var redisConnectionString = Environment.GetEnvironmentVariable("ADSMANAGER_REDIS_CONNECTION")
            ?? cacheOptions.Redis.ConnectionString;

        services.AddMemoryCache();

        if (!useRedis)
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
            return;
        }

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
            return;
        }

        try
        {
            var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        catch when (environment.IsDevelopment())
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
    }
}
