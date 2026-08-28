using System.Collections.Concurrent;
using TradingPlatform.Api.Models;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Thread-safe latest-price store keyed by symbol (case-insensitive — symbols
/// arrive from an external feed; we never trust their casing).
/// ConcurrentDictionary: the feed loop is the only writer; controllers/broadcast
/// read concurrently without locks.
/// </summary>
public class InMemoryPriceStore : IPriceStore
{
    private readonly ConcurrentDictionary<string, PriceTick> _latest =
        new(StringComparer.OrdinalIgnoreCase);

    public void Update(PriceTick tick) => _latest[tick.Symbol] = tick;

    public bool TryGet(string symbol, out PriceTick tick) =>
        _latest.TryGetValue(symbol, out tick!);

    public IReadOnlyList<PriceTick> GetSnapshot() => _latest.Values.ToList();
}