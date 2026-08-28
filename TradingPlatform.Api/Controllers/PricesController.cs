using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Api.Infrastructure.Services;

namespace TradingPlatform.Api.Controllers;

/// <summary>
/// Read-only price snapshot endpoint (assignment §5, Phase 5).
/// Serves the same latest-tick-per-symbol data the SignalR "prices" event
/// streams live — this is the REST fallback/initial-load path (e.g. a
/// dashboard's first GET before the SignalR connection is established).
/// </summary>
[ApiController]
[Route("api/prices")]
public class PricesController : ControllerBase
{
    private readonly IPriceStore _priceStore;

    public PricesController(IPriceStore priceStore)
    {
        _priceStore = priceStore;
    }

    /// <summary>
    /// Latest known tick per symbol. changePct is always null here — that
    /// value only exists relative to the throttle broadcaster's own history
    /// (MarketBroadcastService), which this stateless GET has no access to.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var prices = _priceStore.GetSnapshot()
            .OrderBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(t => new
            {
                symbol = t.Symbol,
                price = t.Price,
                bid = t.Bid,
                ask = t.Ask,
                changePct = (decimal?)null,
                ts = t.ReceivedUtc
            });

        return Ok(prices);
    }
}