using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class AuthLockoutStateConfiguration : IEntityTypeConfiguration<AuthLockoutState>
{
    public void Configure(EntityTypeBuilder<AuthLockoutState> builder)
    {
        builder.ToTable("AuthLockoutStates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => new { x.Email, x.IpAddress }).IsUnique();
        builder.HasIndex(x => x.LockoutUntil);
    }
}
