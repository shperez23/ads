using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class AuthAttemptLogConfiguration : IEntityTypeConfiguration<AuthAttemptLog>
{
    public void Configure(EntityTypeBuilder<AuthAttemptLog> builder)
    {
        builder.ToTable("AuthAttemptLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AttemptType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(250);
        builder.Property(x => x.AttemptedAt).IsRequired();

        builder.HasIndex(x => new { x.AttemptType, x.AttemptedAt });
        builder.HasIndex(x => new { x.Email, x.AttemptedAt });
        builder.HasIndex(x => new { x.IpAddress, x.AttemptedAt });
    }
}
