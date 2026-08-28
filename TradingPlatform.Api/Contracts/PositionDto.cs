namespace TradingPlatform.Api.Contracts;

/// <summary>
/// GET /api/positions response item (assignment §8, Phase 5 Step 22 /
/// trading-platform-plan.md Phase 7 PositionSummary model).
/// AvgPrice is null when NetQuantity is 0 (no open lot). CurrentPrice and
/// UnrealizedPnL are both null when IPriceStore has no live tick for the
/// symbol — never assumed to be zero.
/// </summary>
public sealed record PositionDto(
    string Symbol,
    decimal NetQuantity,
    decimal? AvgPrice,
    decimal? CurrentPrice,
    decimal? UnrealizedPnL,
    decimal RealizedPnL);