using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdsManager.Infrastructure.Persistence.Configurations;

public sealed class RuleExecutionLogConfiguration : IEntityTypeConfiguration<RuleExecutionLog>
{
    public void Configure(EntityTypeBuilder<RuleExecutionLog> builder)
    {
        builder.ToTable("RuleExecutionLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActionExecuted).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MetricValue).HasPrecision(18, 4);
        builder.Property(x => x.Details).HasColumnType("text").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.RuleId, x.ExecutedAt });

        builder.HasOne(x => x.Rule)
            .WithMany(x => x.ExecutionLogs)
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
