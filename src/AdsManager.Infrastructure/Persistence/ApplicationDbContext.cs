using AdsManager.Application.Interfaces;
using AdsManager.Domain.Common;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = utcNow;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = utcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
