namespace PolyEdgeScout.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyEdgeScout.Domain.Entities;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.Timestamp).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.UserId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PropertyName).HasMaxLength(100);
        builder.Property(a => a.OldValue).HasMaxLength(1000);
        builder.Property(a => a.NewValue).HasMaxLength(1000);
        builder.Property(a => a.CorrelationId).HasMaxLength(32).IsRequired();

        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.CorrelationId);
        builder.HasIndex(a => a.Timestamp);
    }
}
