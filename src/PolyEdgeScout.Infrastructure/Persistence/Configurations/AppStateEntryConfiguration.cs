namespace PolyEdgeScout.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyEdgeScout.Domain.Entities;

public sealed class AppStateEntryConfiguration : IEntityTypeConfiguration<AppStateEntry>
{
    public void Configure(EntityTypeBuilder<AppStateEntry> builder)
    {
        builder.ToTable("AppState");
        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.UpdatedAtUtc).IsRequired();
    }
}
