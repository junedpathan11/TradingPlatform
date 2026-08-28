using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Api.Exceptions;
using TradingPlatform.Api.Infrastructure.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TradingPlatform.Api.Controllers;

/// <summary>TEMPORARY Phase 2/3 verification endpoints — remove in Phase 5.</summary>
[ApiController]
[Route("api/authprobe")]
public class AuthProbeController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPriceStore _priceStore;
    private readonly FeedStateService _feedState;

    public AuthProbeController(
        IAuthService authService,
        IPriceStore priceStore,
        FeedStateService feedState)
    {
        _authService = authService;
        _priceStore = priceStore;
        _feedState = feedState;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var token = await _authService.GetTokenAsync(ct);
            return Ok(new
            {
                result = "token acquired",
                tokenPreview = token.Length <= 12
                    ? new string('•', token.Length)
                    : token[..6] + "…" + new string('•', 6),
                length = token.Length,
                utcAt = DateTime.UtcNow
            });
        }
        catch (AuthException ex)
        {
            return StatusCode(502, new { result = "auth failed", error = ex.Message });
        }
    }

    [HttpGet("prices")]
    public IActionResult GetPrices()
    {
        return Ok(new
        {
            feed = new
            {
                state = _feedState.CurrentState.ToString(),
                _feedState.LastError,
                _feedState.LastStateChangedUtc
            },
            prices = _priceStore.GetSnapshot(),
            utcAt = DateTime.UtcNow
        });
    }

    /// <summary>TEMPORARY: probes candidate REST metadata endpoints with the live token
    /// (query-param style first, matching the WS pattern, then Bearer). One-shot diagnostic
    /// to discover the real instrument names/schema — remove in Phase 5.</summary>
    [HttpGet("discover")]
    public async Task<IActionResult> Discover(CancellationToken ct)
    {
        var token = await _authService.GetTokenAsync(ct);
        var candidates = new[]
        {
            "/api/v2/instruments", "/api/v2/symbols", "/api/v2/isins",
            "/api/v2/instrument/list", "/api/v2/market/instruments",
            "/api/v2/quotes", "/api/v2/rates", "/api/v2/prices",
            "/api/v2/config", "/api/v2/user", "/api/v2/accounts", "/api/v2/me"
        };

        var results = new List<object>();
        using var http = new HttpClient { BaseAddress = new Uri("http://s138.acttrader.com:10138"), Timeout = TimeSpan.FromSeconds(10) };

        foreach (var path in candidates)
        {
            // Variant 1: token as query parameter (matches the WS handshake style)
            HttpResponseMessage r1 = null;
            try { r1 = await http.GetAsync($"{path}?token={Uri.EscapeDataString(token)}", ct); } catch { }
            var status1 = r1?.StatusCode.ToString() ?? "ERR";
            var body1 = r1 == null ? "" : Truncate(await r1.Content.ReadAsStringAsync(ct), 250);
            results.Add(new { path, via = "query", status = status1, body = body1 });

            // Variant 2: Bearer header — only bother if query variant wasn't a clean 200
            if (r1 == null || !r1.IsSuccessStatusCode)
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, path);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage r2 = null;
                try { r2 = await http.SendAsync(req, ct); } catch { }
                var status2 = r2?.StatusCode.ToString() ?? "ERR";
                var body2 = r2 == null ? "" : Truncate(await r2.Content.ReadAsStringAsync(ct), 250);
                results.Add(new { path, via = "bearer", status = status2, body = body2 });
            }
        }

        return Ok(results);
    }


    /// <summary>TEMPORARY: fetches the FULL instrument table from the provider —
    /// the authoritative symbol universe for subscriptions. Remove in Phase 5.</summary>
    [HttpGet("instruments")]
    public async Task<IActionResult> Instruments(CancellationToken ct)
    {
        var token = await _authService.GetTokenAsync(ct);
        using var http = new HttpClient
        {
            BaseAddress = new Uri("http://s138.acttrader.com:10138"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        var json = await http.GetStringAsync($"/api/v2/market/instruments?token={Uri.EscapeDataString(token)}", ct);
        return Ok(JsonDocument.Parse(json).RootElement);
    }

    /// <summary>TEMPORARY: hunts for the vendor's "ws-price-feed/config" style endpoint
    /// (FxyFi's terminal fetches its PRICE socket URL from such a route — our /ws may be
    /// the trading-events socket instead). Remove in Phase 5.</summary>
    [HttpGet("feedconfig")]
    public async Task<IActionResult> FeedConfig(CancellationToken ct)
    {
        var token = await _authService.GetTokenAsync(ct);
        var candidates = new[]
        {
            "/api/v2/ws-price-feed/config",
            "/api/ws-price-feed/config",
            "/api/v2/price-feed/config",
            "/api/v2/socket/config",
            "/api/v2/market/config",
            "/api/v2/feed/config",
            "/api/v2/ws/config",
            "/api/v2/pricefeed"
        };

        var results = new List<object>();
        using var http = new HttpClient { BaseAddress = new Uri("http://s138.acttrader.com:10138"), Timeout = TimeSpan.FromSeconds(10) };

        foreach (var path in candidates)
        {
            HttpResponseMessage r = null;
            try { r = await http.GetAsync($"{path}?token={Uri.EscapeDataString(token)}", ct); } catch { }

            var status = r?.StatusCode.ToString() ?? "ERR";
            var body = r == null ? "" : Truncate(await r.Content.ReadAsStringAsync(ct), 400);
            results.Add(new { path, status, body });

            if (r is { IsSuccessStatusCode: true })
            {
                break; // found it — stop hunting
            }
        }

        return Ok(results);
    }
    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
}