namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>The four UI-facing feed states required by assignment §6.3.</summary>
public enum FeedConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error,
}

/// <summary>
/// Process-wide feed connection state (singleton). Pure state holder — no
/// interface by design: it has no behavior to fake; consumers read
/// CurrentState/LastError, the feed service writes via SetState.
/// Written by the feed service (background thread), read by HTTP requests —
/// hence the lock around every access.
/// </summary>
public class FeedStateService
{
    private readonly object _lock = new();
    private FeedConnectionState _state = FeedConnectionState.Disconnected;
    private string? _lastError;
    private DateTime _changedUtc = DateTime.UtcNow;

    public FeedConnectionState CurrentState
    {
        get { lock (_lock) return _state; }
    }

    public string? LastError
    {
        get { lock (_lock) return _lastError; }
    }

    public DateTime LastStateChangedUtc
    {
        get { lock (_lock) return _changedUtc; }
    }

    public void SetState(FeedConnectionState state, string? error = null)
    {
        lock (_lock)
        {
            _state = state;
            _lastError = error;
            _changedUtc = DateTime.UtcNow;
        }
    }
}