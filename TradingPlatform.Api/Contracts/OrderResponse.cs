namespace TradingPlatform.Api.Contracts;

/// <summary>
/// POST /api/orders confirmation response (assignment §6/§8).
/// TradeId is the display format ("TRD10001") — the identity PK stays an int
/// internally (Trade.TradeId); this is purely an API-facing rendering.
/// </summary>
public sealed record OrderResponse(
    string TradeId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal ExecutedPrice,
    string Status,
    DateTime TimestampUtc);