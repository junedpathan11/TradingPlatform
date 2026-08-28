using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TradingPlatform.Api.Infrastructure.Services;

namespace TradingPlatform.Api.Controllers;

/// <summary>
/// Health/status endpoint (assignment §5, Phase 5): reports API liveness,
/// current feed connection state, when the last tick was received, which
/// symbols are currently live, and process uptime — a single glance for
/// dashboards/monitoring without needing SignalR or the price feed logs.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IPriceStore _priceStore;
    private readonly FeedStateService _feedState;

    public HealthController(IPriceStore priceStore, FeedStateService feedState)
    {
        _priceStore = priceStore;
        _feedState = feedState;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var snapshot = _priceStore.GetSnapshot();

        DateTime? lastTickAt = snapshot.Count > 0
            ? snapshot.Max(t => t.ReceivedUtc)
            : null;

        var symbols = snapshot
            .Select(t => t.Symbol)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new
        {
            api = "ok",
            feed = _feedState.CurrentState.ToString(),
            feedError = _feedState.LastError,
            lastTickAt,
            symbols,
            uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
        });
    }
}