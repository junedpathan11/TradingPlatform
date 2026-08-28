namespace TradingPlatform.Api.Infrastructure.Formatting;

/// <summary>
/// Formats a Trade's internal identity PK into the customer-facing display ID
/// used across POST /api/orders and GET /api/trades (docs/assumptions.md D5).
/// Extracted in Phase 9 so the "TRD1xxxx" formula has a single, independently
/// unit-testable definition instead of being duplicated as an inline string
/// interpolation in two controllers.
/// </summary>
public static class TradeIdFormatter
{
    private const int Offset = 10000;

    /// <summary>Formats an identity PK (e.g. 1) as "TRD10001".</summary>
    public static string Format(int tradeId) => $"TRD{Offset + tradeId}";
}