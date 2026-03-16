using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class ApiLogConfiguration : IEntityTypeConfiguration<ApiLog>
{
    public void Configure(EntityTypeBuilder<ApiLog> builder)
    {
        builder.ToTable("ApiLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Endpoint).HasMaxLength(400).IsRequired();
        builder.Property(x => x.Method).HasMaxLength(10).IsRequired();
        builder.Property(x => x.RequestJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ResponseJson).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasIndex(x => new { x.Provider, x.CreatedAt });
    }
}
