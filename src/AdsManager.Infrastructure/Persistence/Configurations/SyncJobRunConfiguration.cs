using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class SyncJobRunConfiguration : IEntityTypeConfiguration<SyncJobRun>
{
    public void Configure(EntityTypeBuilder<SyncJobRun> builder)
    {
        builder.ToTable("SyncJobRun");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Error).HasColumnType("text");

        builder.HasIndex(x => new { x.JobName, x.StartedAt });
    }
}
