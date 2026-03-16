using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class SyncCursorConfiguration : IEntityTypeConfiguration<SyncCursor>
{
    public void Configure(EntityTypeBuilder<SyncCursor> builder)
    {
        builder.ToTable("SyncCursors");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdAccountId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.AdAccountId, x.EntityType }).IsUnique();
    }
}
