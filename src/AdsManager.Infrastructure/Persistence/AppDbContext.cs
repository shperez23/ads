using AdsManager.Application.Interfaces;
using AdsManager.Domain.Common;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence;

public abstract class AppDbContext<TContext> : DbContext, IApplicationDbContext
    where TContext : DbContext
{
    protected AppDbContext(DbContextOptions<TContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MetaConnection> MetaConnections => Set<MetaConnection>();
    public DbSet<AdAccount> AdAccounts => Set<AdAccount>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<AdSet> AdSets => Set<AdSet>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<InsightDaily> InsightsDaily => Set<InsightDaily>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiLog> ApiLogs => Set<ApiLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}