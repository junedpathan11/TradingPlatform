using Microsoft.AspNetCore.SignalR;
using TradingPlatform.Api.Infrastructure.Services;

namespace TradingPlatform.Api.Hubs;

/// <summary>
/// SignalR hub for live price streaming (Phase 4, assignment §6.3/§7).
/// Every client joins the shared "market" group on connect and immediately
/// receives a snapshot of the latest known price per symbol, so a late-joining
/// browser sees prices instantly instead of waiting for the next periodic
/// broadcast. The periodic throttled "prices" batch (default 300ms flush) is
/// added by the broadcaster in Step 17 — this hub only pushes the one-time
/// snapshot for now.
/// No client-invokable methods yet; an optional SubscribePrice(symbol) for
/// targeted updates is deferred per the plan.
/// </summary>
public class MarketHub : Hub
{
    private const string MarketGroup = "market";

    private readonly IPriceStore _priceStore;

    public MarketHub(IPriceStore priceStore)
    {
        _priceStore = priceStore;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, MarketGroup);

        // Snapshot-on-connect. changePct is null here — it requires comparing
        // against a previous flush, which is the throttle broadcaster's job
        // (Step 17). Shape matches the documented "prices" event contract.
        var snapshot = _priceStore.GetSnapshot()
            .Select(t => new
            {
                symbol = t.Symbol,
                price = t.Price,
                changePct = (decimal?)null,
                ts = t.ReceivedUtc
            })
            .ToArray();

        await Clients.Caller.SendAsync("prices", snapshot);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, MarketGroup);
        await base.OnDisconnectedAsync(exception);
    }
}