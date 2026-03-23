namespace PolyEdgeScout.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyEdgeScout.Domain.Entities;

public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasMaxLength(8).IsRequired();
        builder.Property(t => t.MarketQuestion).HasMaxLength(500).IsRequired();
        builder.Property(t => t.ConditionId).HasMaxLength(100).IsRequired();
        builder.Property(t => t.TokenId).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Action).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.EntryPrice).IsRequired();
        builder.Property(t => t.Shares).IsRequired();
        builder.Property(t => t.ModelProbability).IsRequired();
        builder.Property(t => t.Edge).IsRequired();
        builder.Property(t => t.Outlay).IsRequired();
        builder.Property(t => t.Timestamp).IsRequired();
        builder.Property(t => t.IsPaper).IsRequired();
        builder.Property(t => t.TxHash).HasMaxLength(100);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Timestamp);
    }
}
