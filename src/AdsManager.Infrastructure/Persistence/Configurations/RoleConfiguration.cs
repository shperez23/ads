using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private static readonly DateTime SeedDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        var adminId = Guid.Parse("2B88F0A4-CE0F-4372-8039-05C476946C9B");
        var managerId = Guid.Parse("858A5A98-4808-4922-971A-B73EB6BFA523");
        var analystId = Guid.Parse("D76C9FC6-02D3-49A9-A447-CA26DD4C9D8A");

        builder.HasData(
            new Role { Id = adminId, Name = "Admin", Description = "Tenant administrator", CreatedAt = SeedDate },
            new Role { Id = managerId, Name = "Manager", Description = "Campaign manager", CreatedAt = SeedDate },
            new Role { Id = analystId, Name = "Analyst", Description = "Read-only analyst", CreatedAt = SeedDate });
    }
}
