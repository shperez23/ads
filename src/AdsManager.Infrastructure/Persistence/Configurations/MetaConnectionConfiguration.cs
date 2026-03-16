using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class MetaConnectionConfiguration : IEntityTypeConfiguration<MetaConnection>
{
    public void Configure(EntityTypeBuilder<MetaConnection> builder)
    {
        builder.ToTable("MetaConnections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AppId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AppSecret).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AccessToken).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.BusinessId).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.TenantId);
    }
}
