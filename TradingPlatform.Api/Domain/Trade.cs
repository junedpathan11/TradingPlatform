namespace TradingPlatform.Api.Domain;

/// <summary>
/// A single executed trade — the assignment-required storage record (assignment §8).
/// TradeId is an identity PK; the customer-facing id (e.g. "TRD10018") is a display
/// format applied in API DTOs in Phase 5, not in storage.
/// </summary>
public class Trade
{
    /// <summary>Unique identifier (SQL Server INT IDENTITY primary key).</summary>
    public int TradeId { get; set; }

    /// <summary>Instrument selected by the user (e.g. "EURUSD"). Required, max 16 chars.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Order side: Buy or Sell. Stored as string ("Buy"/"Sell"), CHECK-constrained.</summary>
    public TradeSide Side { get; set; }

    /// <summary>Quantity — must be positive. decimal(18,2).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Latest live price at execution. decimal(18,5) — fits 5-decimal FX quotes.</summary>
    public decimal Price { get; set; }

    /// <summary>Server (UTC) execution time — assignment §8 "server time preferred". datetime2(3).</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Simple status per assignment §8: Filled or Rejected. CHECK-constrained.</summary>
    public TradeStatus Status { get; set; }
}