namespace PolyEdgeScout.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyEdgeScout.Domain.Entities;

public sealed class TradeResultConfiguration : IEntityTypeConfiguration<TradeResult>
{
    public void Configure(EntityTypeBuilder<TradeResult> builder)
    {
        builder.ToTable("TradeResults");
        builder.HasKey(r => r.TradeId);

        builder.Property(r => r.TradeId).HasMaxLength(8).IsRequired();
        builder.Property(r => r.MarketQuestion).HasMaxLength(500).IsRequired();
        builder.Property(r => r.EntryPrice).IsRequired();
        builder.Property(r => r.Shares).IsRequired();
        builder.Property(r => r.GrossProfit).IsRequired();
        builder.Property(r => r.Fees).IsRequired();
        builder.Property(r => r.Gas).IsRequired();
        builder.Ignore(r => r.NetProfit); // Computed property — not persisted
        builder.Property(r => r.Roi).IsRequired();
        builder.Property(r => r.SettledAt).IsRequired();
        builder.Property(r => r.Won).IsRequired();
    }
}
