namespace TradingPlatform.Api.Models;

/// <summary>
/// Latest known quote for one symbol (in-memory only — never persisted; see
/// docs/assumptions.md D2). Bid/Ask are nullable until the live message schema
/// confirms which fields arrive (docs/assumptions.md A4/U4).
/// </summary>
public sealed record PriceTick(
    string Symbol,
    decimal Price,
    decimal? Bid,
    decimal? Ask,
    DateTime ReceivedUtc);