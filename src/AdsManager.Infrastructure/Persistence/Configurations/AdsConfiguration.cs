using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class AdsConfiguration : IEntityTypeConfiguration<AdAccount>, IEntityTypeConfiguration<Campaign>, IEntityTypeConfiguration<AdSet>, IEntityTypeConfiguration<Ad>, IEntityTypeConfiguration<InsightDaily>, IEntityTypeConfiguration<MetaConnection>, IEntityTypeConfiguration<AuditLog>, IEntityTypeConfiguration<ApiLog>
{
    public void Configure(EntityTypeBuilder<AdAccount> builder)
    {
        builder.ToTable("AdAccounts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.MetaAccountId }).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }

    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.AdAccount).WithMany(x => x.Campaigns).HasForeignKey(x => x.AdAccountId);
        builder.HasIndex(x => new { x.TenantId, x.MetaCampaignId }).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }

    public void Configure(EntityTypeBuilder<AdSet> builder)
    {
        builder.ToTable("AdSets");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Campaign).WithMany(x => x.AdSets).HasForeignKey(x => x.CampaignId);
        builder.HasIndex(x => new { x.TenantId, x.MetaAdSetId }).IsUnique();
        builder.Property(x => x.TargetingJson).HasColumnType("jsonb");
    }

    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.ToTable("Ads");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.AdSet).WithMany(x => x.Ads).HasForeignKey(x => x.AdSetId);
        builder.HasIndex(x => new { x.TenantId, x.MetaAdId }).IsUnique();
        builder.Property(x => x.CreativeJson).HasColumnType("jsonb");
    }

    public void Configure(EntityTypeBuilder<InsightDaily> builder)
    {
        builder.ToTable("InsightsDaily");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.Date, x.AdAccountId });
        builder.Property(x => x.ConversionsJson).HasColumnType("jsonb");
    }

    public void Configure(EntityTypeBuilder<MetaConnection> builder)
    {
        builder.ToTable("MetaConnections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AppSecret).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AccessToken).HasMaxLength(3000).IsRequired();
        builder.HasIndex(x => x.TenantId);
    }

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.CreatedAt });
    }

    public void Configure(EntityTypeBuilder<ApiLog> builder)
    {
        builder.ToTable("ApiLogs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.Provider, x.CreatedAt });
    }
}
