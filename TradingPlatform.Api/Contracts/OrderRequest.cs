namespace TradingPlatform.Api.Contracts;

/// <summary>
/// POST /api/orders request body (assignment §6/§8).
/// Side is a string ("Buy"/"Sell", case-insensitive) to match the documented
/// contract — parsed and validated against TradeSide in the controller.
/// </summary>
public sealed record OrderRequest(string Symbol, string Side, decimal Quantity);