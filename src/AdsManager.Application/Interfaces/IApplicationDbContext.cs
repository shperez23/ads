using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<MetaConnection> MetaConnections { get; }
    DbSet<AdAccount> AdAccounts { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<AdSet> AdSets { get; }
    DbSet<Ad> Ads { get; }
    DbSet<InsightDaily> InsightDaily { get; }
    DbSet<InsightDaily> InsightsDaily { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ApiLog> ApiLogs { get; }
    DbSet<SyncCursor> SyncCursors { get; }
    DbSet<SyncJobRun> SyncJobRuns { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
