using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class AdsConfiguration : IEntityTypeConfiguration<Ad>
{
    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.ToTable("Ads");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetaAdId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreativeJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.PreviewUrl).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MetaAdId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.AdSetId });

        builder.HasOne(x => x.AdSet)
            .WithMany(x => x.Ads)
            .HasForeignKey(x => x.AdSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
