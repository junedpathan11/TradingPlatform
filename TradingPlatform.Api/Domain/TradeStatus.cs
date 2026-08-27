namespace TradingPlatform.Api.Domain;

/// <summary>Trade execution status per assignment §8 ("simple status is acceptable"). Persisted as string.</summary>
public enum TradeStatus
{
    Filled = 1,
    Rejected = 2,
}