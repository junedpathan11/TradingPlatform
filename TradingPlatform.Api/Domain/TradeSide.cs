namespace TradingPlatform.Api.Domain;

/// <summary>Order side. Persisted as its string name ("Buy"/"Sell") via value conversion.</summary>
public enum TradeSide
{
    Buy = 1,
    Sell = 2,
}