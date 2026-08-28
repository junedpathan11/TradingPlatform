namespace TradingPlatform.Api.Contracts;

/// <summary>
/// GET /api/trades response item (assignment §8, Phase 5 Step 21).
/// TradeId is the display format ("TRD10001"), matching OrderResponse from
/// POST /api/orders (docs/assumptions.md D5) — the identity PK stays an int
/// internally (Trade.TradeId).
/// </summary>
public sealed record TradeDto(
    string TradeId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal Price,
    string Status,
    DateTime TimestampUtc);