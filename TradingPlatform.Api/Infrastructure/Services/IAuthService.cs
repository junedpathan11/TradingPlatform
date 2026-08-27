namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Provider authentication: obtains and caches the WebSocket-auth token
/// from the provider REST auth endpoint (assignment §3).
/// </summary>
public interface IAuthService
{
    /// <summary>Returns a valid provider token, from cache when possible.</summary>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops the cached token so the next call re-authenticates (e.g. after a WS 401).</summary>
    void InvalidateToken();
}