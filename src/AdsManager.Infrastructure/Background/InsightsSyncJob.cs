using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AdsManager.Infrastructure.Background;

public sealed class InsightsSyncJob
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMetaAdsService _metaAdsService;

    public InsightsSyncJob(IApplicationDbContext dbContext, IMetaAdsService metaAdsService)
    {
        _dbContext = dbContext;
        _metaAdsService = metaAdsService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var connections = await _dbContext.MetaConnections.AsNoTracking()
            .Where(x => x.Status == Domain.Enums.ConnectionStatus.Connected)
            .ToListAsync(cancellationToken);

        foreach (var connection in connections)
        {
            var accounts = await _dbContext.AdAccounts.AsNoTracking().Where(x => x.TenantId == connection.TenantId).ToListAsync(cancellationToken);
            foreach (var account in accounts)
            {
                await _metaAdsService.GetInsightsAsync(account.MetaAccountId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), connection.AccessToken, cancellationToken);
                Log.Information("Insights synced for Tenant {TenantId} Account {AccountId}", connection.TenantId, account.MetaAccountId);
            }
        }
    }
}
