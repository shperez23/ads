using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class InsightDailyConfiguration : IEntityTypeConfiguration<InsightDaily>
{
    public void Configure(EntityTypeBuilder<InsightDaily> builder)
    {
        builder.ToTable("InsightsDaily");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Spend).HasPrecision(18, 2);
        builder.Property(x => x.Cpm).HasPrecision(18, 4);
        builder.Property(x => x.Cpc).HasPrecision(18, 4);
        builder.Property(x => x.Ctr).HasPrecision(18, 4);
        builder.Property(x => x.ConversionsJson).HasColumnType("text").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.AdAccountId, x.Date });
        builder.HasIndex(x => new { x.TenantId, x.CampaignId, x.Date });
        builder.HasIndex(x => new { x.TenantId, x.Date, x.CampaignId });
    }
}
