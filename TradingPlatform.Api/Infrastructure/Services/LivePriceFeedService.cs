using Microsoft.Extensions.Options;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TradingPlatform.Api.Options;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Live provider price feed (assignment §6.1): obtains a token from IAuthService,
/// connects to ws://…/ws?token=…, subscribes to instruments, parses incoming
/// messages into IPriceStore, and reconnects with exponential backoff + jitter on
/// any drop. A 401 at handshake invalidates the cached token so the next attempt
/// re-authenticates (D8). Everything run inside the loop is caught — the host must
/// never crash because the feed hiccupped (assignment §9: graceful handling).
/// </summary>
public class LivePriceFeedService : BackgroundService
{
    private readonly IAuthService _authService;
    private readonly IPriceStore _priceStore;
    private readonly FeedStateService _feedState;
    private readonly FeedOptions _options;
    private readonly ILogger<LivePriceFeedService> _logger;
    private int _rawSamplesLogged;
    private int _heartbeatCount;
    private bool _firstTickSeen;

    public LivePriceFeedService(
        IAuthService authService,
        IPriceStore priceStore,
        FeedStateService feedState,
        IOptions<FeedOptions> options,
        ILogger<LivePriceFeedService> logger)
    {
        _authService = authService;
        _priceStore = priceStore;
        _feedState = feedState;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _feedState.SetState(FeedConnectionState.Disconnected);
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionAsync(stoppingToken).ConfigureAwait(false);
                attempt = 0; // connection ended cleanly — reconnect fast
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // app shutdown — exit quietly
            }
            catch (Exception ex)
            {
                attempt++;
                var unauthorized = IsUnauthorizedFailure(ex);

                if (unauthorized)
                {
                    _logger.LogWarning(
                        "WS handshake rejected (401) — token invalid/expired. Forcing re-auth on next attempt.");
                    _authService.InvalidateToken(); // D8: next GetTokenAsync performs a fresh Digest login
                    _feedState.SetState(FeedConnectionState.Error, "auth rejected (401) — re-authenticating");
                }
                else
                {
                    _feedState.SetState(FeedConnectionState.Error, ex.Message);
                }

                var delay = BackoffDelay(attempt);
                _logger.LogWarning(ex,
                    "WS connection attempt {Attempt} failed (unauthorized={Unauthorized}). Reconnecting in {Delay}.",
                    attempt, unauthorized, delay);

                try
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _feedState.SetState(FeedConnectionState.Disconnected);
    }

    /// <summary>One full connection lifecycle: auth → connect → subscribe → receive loop until close/failure.</summary>
    private async Task RunConnectionAsync(CancellationToken ct)
    {
        _feedState.SetState(FeedConnectionState.Connecting);

        var token = await _authService.GetTokenAsync(ct).ConfigureAwait(false);
        var uri = new Uri($"{_options.WsUrl}?token={Uri.EscapeDataString(token)}");

        using var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30); // protocol-level pings keep the socket healthy

        await ws.ConnectAsync(uri, ct).ConfigureAwait(false); // throws on failed handshake (e.g. 401)

        _feedState.SetState(FeedConnectionState.Connected);

        // Subscribe — PROTOCOL DISCOVERY: the vendor watchlist-frame shape was
        // silently ignored even with valid instruments, so probe 5 candidate
        // message formats (3 s apart) and log everything that comes back.
        // Temporary; the winning format gets cleaned up once identified.
        if (_options.Symbols is { Length: > 0 })
        {
            var names = _options.Symbols.Distinct().ToArray();
            var first = names[0];

            var candidates = new (string Label, string Frame)[]
            {
                ("V1 action/symbols",  JsonSerializer.Serialize(new { action = "subscribe", symbols = names })),
                ("V2 subscribe:string", JsonSerializer.Serialize(new { subscribe = first })),
                ("V3 cmd/isin",        JsonSerializer.Serialize(new { cmd = "subscribe", isin = first })),
                ("V4 event/symbols",   JsonSerializer.Serialize(new { @event = "subscribe", symbols = names })),
                ("V5 plain text",      $"subscribe {first}"),
            };

            foreach (var (label, frame) in candidates)
            {
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(frame)),
                    WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
                _logger.LogInformation("SUBSCRIBE PROBE [{Label}]: {Frame}", label, frame);

                try { await Task.Delay(3000, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
        }

        _logger.LogInformation("WS connected to {Host}:{Port}. Awaiting price messages…",
            uri.Host, uri.Port);

        var buffer = new byte[8192];
        using var messageBytes = new MemoryStream();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            messageBytes.SetLength(0);

            // A logical message may arrive across multiple frames — accumulate until EndOfMessage.
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Server closed the WS ({Status}: {Description}). Reconnecting.",
                        result.CloseStatus, result.CloseStatusDescription);
                    await CloseGracefullyAsync(ws, ct).ConfigureAwait(false);
                    return;
                }
                messageBytes.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            var raw = Encoding.UTF8.GetString(messageBytes.ToArray());

            if (result.MessageType != WebSocketMessageType.Text)
            {
                _logger.LogInformation(
                    "WS BINARY frame received ({Bytes} bytes): {Head}",
                    messageBytes.Length,
                    Convert.ToHexString(buffer, 0, Math.Min(buffer.Length, 32)));
                continue;
            }

            if (string.Equals(raw.Trim(), "heartbeat", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _heartbeatCount);
                if (_heartbeatCount % 10 == 1)
                {
                    _logger.LogInformation("Server heartbeats received so far: {Count}", _heartbeatCount);
                }
                await EchoHeartbeatAsync(ws, ct).ConfigureAwait(false);
                continue;
            }

            HandleRawMessage(raw);
        }
    }

    private async Task EchoHeartbeatAsync(ClientWebSocket ws, CancellationToken ct)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes("heartbeat");
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug("Heartbeat echo failed: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Parses → store. Logs raw verbatim until the first tick is seen (schema
    /// capture for U4), so the real provider message shape is recorded. Never throws.
    /// </summary>
    private void HandleRawMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        if (!_firstTickSeen && Volatile.Read(ref _rawSamplesLogged) < 50)
        {
            Interlocked.Increment(ref _rawSamplesLogged);
            _logger.LogInformation("RAW WS MESSAGE ({N}): {Raw}",
                _rawSamplesLogged, raw.Length > 500 ? raw[..500] + "…" : raw);
        }

        if (PriceMessageParser.TryParse(raw, out var tick))
        {
            if (!_firstTickSeen)
            {
                _firstTickSeen = true;
                _logger.LogInformation(
                    "FIRST TICK PARSED: {Symbol} @ {Price} (bid {Bid} / ask {Ask})",
                    tick.Symbol, tick.Price, tick.Bid, tick.Ask);
            }
            _priceStore.Update(tick);
        }
        else
        {
            _logger.LogDebug("Skipped non-tick WS message: {Raw}",
                raw.Length > 200 ? raw[..200] + "…" : raw);
        }
    }

    /// <summary>Exponential backoff with jitter: base × 2^(attempt-1), capped, plus 0–500 ms jitter.</summary>
    private TimeSpan BackoffDelay(int attempt)
    {
        var cappedAttempt = Math.Min(attempt - 1, 10);
        var exponential = (long)Math.Min(
            (long)_options.ReconnectBaseDelayMs * (1L << cappedAttempt),
            _options.ReconnectMaxDelayMs);
        return TimeSpan.FromMilliseconds(exponential + Random.Shared.Next(0, 500));
    }

    /// <summary>True when the failure chain indicates an HTTP 401 at handshake (token problem, not network).</summary>
    private static bool IsUnauthorizedFailure(Exception ex)
    {
        for (var e = (Exception?)ex; e is not null; e = e.InnerException)
        {
            if (e is HttpRequestException hre && hre.StatusCode == HttpStatusCode.Unauthorized)
            {
                return true;
            }

            if (e.Message.Contains("401"))
            {
                return true;
            }
        }
        return false;
    }

    private static async Task CloseGracefullyAsync(ClientWebSocket ws, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closing", timeout.Token)
                .ConfigureAwait(false);
        }
        catch
        {
            // best-effort close; the reconnect loop owns recovery
        }
    }
}