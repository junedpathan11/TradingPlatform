using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingPlatform.Api.Exceptions;
using TradingPlatform.Api.Options;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Calls the provider REST auth endpoint (POST credentials → token), caches the
/// token in memory, and re-authenticates when the cache is empty/expired/invalidated.
/// Parsing is deliberately tolerant: token field name and response envelope are
/// provider assumptions (docs/assumptions.md A1/A2) until real credentials unblock probes.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthApiOptions _options;
    private readonly ILogger<AuthService> _logger;

    private readonly object _cacheLock = new();
    private string? _cachedToken;
    private DateTime _cachedUntilUtc = DateTime.MinValue;

    public AuthService(HttpClient httpClient, IOptions<AuthApiOptions> options, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _cachedUntilUtc)
            {
                return _cachedToken;
            }
        }

        var result = await RequestTokenAsync(cancellationToken).ConfigureAwait(false);

        lock (_cacheLock)
        {
            _cachedToken = result.Token;
            // Cache for the provider-stated lifetime (minus a 60s safety margin) when
            // available; otherwise keep until InvalidateToken() is called.
            _cachedUntilUtc = result.ExpiresInSec is int sec
                ? DateTime.UtcNow.AddSeconds(sec - 60)
                : DateTime.MaxValue;
        }

        return result.Token;
    }

    public void InvalidateToken()
    {
        lock (_cacheLock)
        {
            _cachedToken = null;
            _cachedUntilUtc = DateTime.MinValue;
        }
    }

    private async Task<TokenResult> RequestTokenAsync(CancellationToken ct)
    {
        _logger.LogInformation("Requesting auth token from {BaseUrl}{TokenPath}",
            _options.BaseUrl, _options.TokenPath);

        using var response = await _httpClient.PostAsJsonAsync(
            _options.TokenPath,
            new TokenRequest(_options.Username, _options.Password),   // assumption A1
            ct).ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Auth token request failed: HTTP {Status}, body: {Body}",
                (int)response.StatusCode, Truncate(body, 500));
            throw new AuthException(
                $"Provider auth failed with HTTP {(int)response.StatusCode}.", (int)response.StatusCode);
        }

        var parsed = ExtractToken(body);
        _logger.LogInformation("Auth token acquired (length {Length}, expiresInSec={Expires}).",
            parsed.Token.Length, parsed.ExpiresInSec?.ToString() ?? "n/a");
        return parsed;
    }

    /// <summary>
    /// Tolerant token extraction (assumption A2): accepts token/accessToken/access_token
    /// at the root or under a "data" wrapper, case-insensitive; recognizes the
    /// {"success":false,...} failure envelope confirmed in Phase 0.
    /// </summary>
    private TokenResult ExtractToken(string body)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new AuthException("Provider auth response is not valid JSON.", inner: ex);
        }

        using (doc)
        {
            var props = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                props[p.Name] = p.Value;
            }

            if (props.TryGetValue("success", out var success) &&
                success.ValueKind == JsonValueKind.False)
            {
                var message = props.TryGetValue("message", out var m) ? m.GetString() : "unknown error";
                throw new AuthException($"Provider auth rejected: {message}");
            }

            if (TryFindToken(props) is { } direct)
            {
                return new TokenResult(direct.Value.GetString(), TryGetExpiresIn(props));
            }

            if (props.TryGetValue("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                var inner = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in data.EnumerateObject())
                {
                    inner[p.Name] = p.Value;
                }

                if (TryFindToken(inner) is { } wrapped)
                {
                    return new TokenResult(wrapped.Value.GetString(), TryGetExpiresIn(inner));
                }
            }

            _logger.LogError(
                "Auth response had no recognizable token field. Body: {Body}", Truncate(body, 500));
            throw new AuthException(
                "Provider auth response did not contain a token field " +
                "(checked token/accessToken/access_token, including a 'data' wrapper).");
        }
    }

    private static KeyValuePair<string, JsonElement>? TryFindToken(Dictionary<string, JsonElement> props)
    {
        foreach (var name in new[] { "token", "accessToken", "access_token" })
        {
            if (props.TryGetValue(name, out var v) && v.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(v.GetString()))
            {
                return new KeyValuePair<string, JsonElement>(name, v);
            }
        }
        return null;
    }

    private static int? TryGetExpiresIn(Dictionary<string, JsonElement> props) =>
        props.TryGetValue("expiresIn", out var e) && e.TryGetInt32(out var s) ? s
        : props.TryGetValue("expires_in", out var e2) && e2.TryGetInt32(out var s2) ? s2 : null;

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";

    private sealed record TokenRequest(string Username, string Password);
    private sealed record TokenResult(string Token, int? ExpiresInSec);
}