using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Api.Domain;

namespace TradingPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API mapping for <see cref="Trade"/> → dbo.Trades.
/// Data-layer integrity only (required fields, lengths, precision, enum string
/// storage, CHECK constraints). Business rules (symbol validity against the live
/// feed, quantity caps, etc.) belong to the order service in a later phase.
/// </summary>
public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades", t =>
        {
            t.HasCheckConstraint("CK_Trades_Quantity_Positive", "[Quantity] > 0");
            t.HasCheckConstraint("CK_Trades_Side", "[Side] IN ('Buy', 'Sell')");
            t.HasCheckConstraint("CK_Trades_Status", "[Status] IN ('Filled', 'Rejected')");
        });

        builder.HasKey(x => x.TradeId);

        builder.Property(x => x.TradeId)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Symbol)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(x => x.Side)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 2);

        builder.Property(x => x.Price)
            .HasPrecision(18, 5);

        builder.Property(x => x.TimestampUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(x => x.Symbol);

        builder.HasIndex(x => x.TimestampUtc);
    }
}