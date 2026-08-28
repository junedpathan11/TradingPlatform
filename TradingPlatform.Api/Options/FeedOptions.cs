namespace TradingPlatform.Api.Options;

/// <summary>
/// Strongly typed "Feed" configuration section (appsettings.json).
/// The token is NOT configured here — it is obtained at runtime from IAuthService
/// and appended to the WS URL (no secrets in config files).
/// </summary>
public sealed class FeedOptions
{
    public const string SectionName = "Feed";

    /// <summary>Provider WebSocket endpoint (assignment §3) without the token query param.</summary>
    public string WsUrl { get; set; } = "ws://s138.acttrader.com:22138/ws";

    /// <summary>Reconnect backoff: first delay, doubling up to Max (with jitter).</summary>
    public int ReconnectBaseDelayMs { get; set; } = 1000;

    public int ReconnectMaxDelayMs { get; set; } = 30000;

    /// <summary>"Live" (provider WS) or "Mock" (demo fallback emitting synthetic ticks —
    /// disclosed in delivery docs; live is the default now that auth works).</summary>
    public string Mode { get; set; } = "Live";
    /// <summary>Instruments to subscribe to after connecting (assignment mockup's set).</summary>
    public string[] Symbols { get; set; } = Array.Empty<string>();
}