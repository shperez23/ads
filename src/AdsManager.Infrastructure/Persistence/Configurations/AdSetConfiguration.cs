using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class AdSetConfiguration : IEntityTypeConfiguration<AdSet>
{
    public void Configure(EntityTypeBuilder<AdSet> builder)
    {
        builder.ToTable("AdSets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetaAdSetId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Budget).HasPrecision(18, 2);
        builder.Property(x => x.BillingEvent).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OptimizationGoal).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BidStrategy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetingJson).HasColumnType("text").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.MetaAdSetId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CampaignId });

        builder.HasOne(x => x.Campaign)
            .WithMany(x => x.AdSets)
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
