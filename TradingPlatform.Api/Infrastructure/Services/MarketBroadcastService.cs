using Microsoft.AspNetCore.SignalR;
using TradingPlatform.Api.Hubs;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Throttled SignalR broadcaster (Phase 4, Step 17). Decoupled from the feed
/// services on purpose: it only reads IPriceStore, so it works identically
/// whether Feed:Mode is Live or Mock, and neither feed service needed changes.
/// Every FlushInterval it diffs the current snapshot against the prices it
/// last broadcast and sends only the symbols that changed, as a single batched
/// "prices" event to the "market" group (see MarketHub) — never per-tick spam.
/// Payload contract (assignment §6.3 / trading-platform-plan.md Phase 4):
/// { "prices": [ { "symbol": "EURUSD", "price": 1.08348, "changePct": 0.12, "ts": "…" } ] }
/// changePct is null the first time a symbol is ever broadcast (no prior
/// value to compare against).
/// </summary>
public class MarketBroadcastService : BackgroundService
{
    // Must match the group name clients join in MarketHub.OnConnectedAsync.
    private const string MarketGroup = "market";
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(300);

    private readonly IPriceStore _priceStore;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<MarketBroadcastService> _logger;

    // Single writer (this service's own loop) — plain Dictionary is fine, no locking needed.
    private readonly Dictionary<string, decimal> _lastBroadcastPrices =
        new(StringComparer.OrdinalIgnoreCase);

    public MarketBroadcastService(
        IPriceStore priceStore,
        IHubContext<MarketHub> hubContext,
        ILogger<MarketBroadcastService> logger)
    {
        _priceStore = priceStore;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    break; // timer disposed
                }

                await FlushAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // app shutdown
            }
            catch (Exception ex)
            {
                // Never let a broadcast failure kill the loop (assignment §9: graceful handling).
                _logger.LogWarning(ex, "Market broadcast flush failed; will retry next tick.");
            }
        }
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        var snapshot = _priceStore.GetSnapshot();
        var changed = new List<object>();

        foreach (var tick in snapshot)
        {
            var hasPrevious = _lastBroadcastPrices.TryGetValue(tick.Symbol, out var previousPrice);

            if (hasPrevious && previousPrice == tick.Price)
            {
                continue; // unchanged since last flush — throttle contract: skip it
            }

            decimal? changePct = (hasPrevious && previousPrice != 0)
                ? Math.Round((tick.Price - previousPrice) / previousPrice * 100m, 4)
                : null;

            changed.Add(new
            {
                symbol = tick.Symbol,
                price = tick.Price,
                changePct,
                ts = tick.ReceivedUtc
            });

            _lastBroadcastPrices[tick.Symbol] = tick.Price;
        }

        if (changed.Count == 0)
        {
            return; // nothing changed this interval — don't spam an empty batch
        }

        await _hubContext.Clients.Group(MarketGroup)
            .SendAsync("prices", changed.ToArray(), ct)
            .ConfigureAwait(false);
    }
}