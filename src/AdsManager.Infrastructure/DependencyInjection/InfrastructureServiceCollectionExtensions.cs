using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Infrastructure.Background;
using AdsManager.Infrastructure.Integrations.Meta;
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

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured");

        services.AddDbContext<AdsManagerDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<AdsManagerDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddDataProtection();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ISecretEncryptionService, SecretEncryptionService>();

        services.AddHttpClient<IMetaAdsService, MetaAdsService>();
        services.AddHttpClient<IMetaConnectionApiClient, MetaConnectionApiClient>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IAdSetRepository, AdSetRepository>();
        services.AddScoped<IAdRepository, AdRepository>();
        services.AddScoped<IInsightRepository, InsightRepository>();
        services.AddScoped<IMetaConnectionRepository, MetaConnectionRepository>();

        services.AddScoped<SyncCampaignsJob>();
        services.AddScoped<SyncInsightsJob>();

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
