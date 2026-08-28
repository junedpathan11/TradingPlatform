using TradingPlatform.Api.Models;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// In-memory latest-price store (assignment §9). Single writer (the active feed
/// service), many readers (controllers, SignalR broadcasts, order execution).
/// </summary>
public interface IPriceStore
{
    /// <summary>Inserts or overwrites the latest tick for the symbol.</summary>
    void Update(PriceTick tick);

    /// <summary>Point read for order execution (Phase 5) — O(1).</summary>
    bool TryGet(string symbol, out PriceTick tick);

    /// <summary>Current latest tick per symbol (unordered copy).</summary>
    IReadOnlyList<PriceTick> GetSnapshot();
}