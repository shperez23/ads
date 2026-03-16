using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetaCampaignId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Objective).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DailyBudget).HasPrecision(18, 2);
        builder.Property(x => x.LifetimeBudget).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.MetaCampaignId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.AdAccountId });

        builder.HasOne(x => x.AdAccount)
            .WithMany(x => x.Campaigns)
            .HasForeignKey(x => x.AdAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
