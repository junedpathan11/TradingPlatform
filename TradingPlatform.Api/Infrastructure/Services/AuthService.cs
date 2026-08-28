using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradingPlatform.Api.Exceptions;
using TradingPlatform.Api.Options;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Calls the provider REST authentication endpoint, obtains an API token,
/// caches the token in memory, and re-authenticates when the token is
/// empty, expired, or invalidated.
///
/// Provider authentication facts (see docs/api-investigation.md):
/// - The endpoint answers with an HTTP Digest challenge (realm "Trade station").
/// - The server's Digest implementation supports ONLY MD5 and crashes with
///   HTTP 500 (Ada CONSTRAINT_ERROR) if the Authorization header carries any
///   other algorithm token. .NET's HttpClientHandler defaults to SHA-256 when
///   the challenge omits an algorithm, so the handshake is performed MANUALLY
///   here with MD5 (RFC 2617, qop=auth) instead of handler-based credentials.
/// - The Digest identity (username) is AuthApi:Username from user-secrets
///   (currently the Account ID; switchable to the User ID without code changes).
/// - Requests send an empty JSON body "{}": identity is carried by the
///   Authorization header, and "{}" is the only body the server's parser has
///   proven to handle without error.
///
/// Token response parsing remains tolerant because the provider response
/// schema is not fully documented (docs/assumptions.md A2).
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthApiOptions _options;
    private readonly ILogger<AuthService> _logger;

    private readonly object _cacheLock = new();

    private string? _cachedToken;
    private DateTime _cachedUntilUtc = DateTime.MinValue;

    public AuthService(
        HttpClient httpClient,
        IOptions<AuthApiOptions> options,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            if (!string.IsNullOrEmpty(_cachedToken) &&
                DateTime.UtcNow < _cachedUntilUtc)
            {
                return _cachedToken;
            }
        }

        var result = await RequestTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        lock (_cacheLock)
        {
            _cachedToken = result.Token;

            // Cache for the provider-stated lifetime minus a 60-second
            // safety margin when an expiry value is available.
            //
            // If the provider does not return an expiry value, the token
            // remains cached until InvalidateToken() is called.
            _cachedUntilUtc = result.ExpiresInSec is int sec
                ? DateTime.UtcNow.AddSeconds(Math.Max(0, sec - 60))
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

        _logger.LogInformation("Cached authentication token invalidated.");
    }

    private async Task<TokenResult> RequestTokenAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Requesting auth token from {BaseUrl}{TokenPath}.",
            _options.BaseUrl,
            _options.TokenPath);

        // ---- Phase 1: unauthenticated request to obtain the Digest challenge.
        //      The body is irrelevant to the server at this stage (it answers
        //      401 + challenge before reading it); "{}" is the safest payload. ----
        using var firstResponse = await _httpClient.PostAsync(
            _options.TokenPath,
            CreateEmptyJsonContent(),
            cancellationToken).ConfigureAwait(false);

        var firstBody = await firstResponse.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (firstResponse.IsSuccessStatusCode)
        {
            return ExtractToken(firstBody);
        }

        if ((int)firstResponse.StatusCode != 401)
        {
            _logger.LogWarning(
                "Auth token request failed: HTTP {Status}, body: {Body}",
                (int)firstResponse.StatusCode,
                Truncate(firstBody, 500));

            throw new AuthException(
                $"Provider auth failed with HTTP {(int)firstResponse.StatusCode}.",
                (int)firstResponse.StatusCode);
        }

        var challengeHeader =
            firstResponse.Headers.WwwAuthenticate.FirstOrDefault(w =>
                string.Equals(w.Scheme, "Digest", StringComparison.OrdinalIgnoreCase))
                ?.Parameter;

        if (string.IsNullOrEmpty(challengeHeader))
        {
            throw new AuthException(
                "Provider returned 401 without a Digest challenge.");
        }

        // ---- Phase 2: answer the challenge with an MD5 digest (the only algorithm
        //      this server implements — anything else crashes it with HTTP 500).
        //      Digest identity = AuthApi:Username from secrets. ----
        var challenge = ParseChallenge(challengeHeader);

        if (!challenge.TryGetValue("realm", out var realm) ||
            !challenge.TryGetValue("nonce", out var nonce))
        {
            throw new AuthException(
                "Provider Digest challenge is missing realm/nonce.");
        }

        var qop = challenge.TryGetValue("qop", out var q) ? q : "auth";
        var nc = "00000001";
        var cnonce = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();

        var ha1 = Md5Hex($"{_options.Username}:{realm}:{_options.Password}");
        var ha2 = Md5Hex($"POST:{_options.TokenPath}");
        var response = Md5Hex($"{ha1}:{nonce}:{nc}:{cnonce}:{qop}:{ha2}");

        var digestHeader =
            $"Digest username=\"{_options.Username}\", " +
            $"realm=\"{realm}\", " +
            $"nonce=\"{nonce}\", " +
            $"uri=\"{_options.TokenPath}\", " +
            "algorithm=MD5, " +
            $"response=\"{response}\", " +
            $"qop={qop}, " +
            $"nc={nc}, " +
            $"cnonce=\"{cnonce}\"";

        if (challenge.TryGetValue("opaque", out var opaque))
        {
            digestHeader += $", opaque=\"{opaque}\"";
        }

        // Raw header delivery: AuthenticationHeaderValue's constructor rejects the
        // composite Digest string (scheme-vs-parameter parsing), and we want the
        // exact byte format proven to pass the server's strict parser.
        using var requestMessage = new HttpRequestMessage(
            HttpMethod.Post,
            _options.TokenPath)
        {
            Content = CreateEmptyJsonContent()
        };

        requestMessage.Headers.TryAddWithoutValidation(
            "Authorization",
            digestHeader);

        using var secondResponse = await _httpClient.SendAsync(
            requestMessage,
            cancellationToken).ConfigureAwait(false);

        var secondBody = await secondResponse.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!secondResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Digest auth attempt failed: HTTP {Status}, body: {Body}",
                (int)secondResponse.StatusCode,
                Truncate(secondBody, 500));

            throw new AuthException(
                $"Provider auth failed with HTTP {(int)secondResponse.StatusCode}.",
                (int)secondResponse.StatusCode);
        }

        var parsed = ExtractToken(secondBody);

        _logger.LogInformation(
            "Auth token acquired (length {Length}, expiresInSec={Expires}).",
            parsed.Token.Length,
            parsed.ExpiresInSec?.ToString() ?? "n/a");

        return parsed;
    }

    /// <summary>Empty JSON body — the only payload the provider's parser handles without error.</summary>
    private static StringContent CreateEmptyJsonContent() =>
        new("{}", Encoding.UTF8, "application/json");

    /// <summary>Parses a WWW-Authenticate Digest challenge into key/value pairs (quotes stripped).</summary>
    private static Dictionary<string, string> ParseChallenge(string challengeHeader)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in challengeHeader.Split(','))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = part[..idx].Trim();
            var value = part[(idx + 1)..].Trim().Trim('"');
            result[key] = value;
        }

        return result;
    }

    /// <summary>Lowercase hex MD5 — the only digest algorithm this provider implements.</summary>
    private static string Md5Hex(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Extracts the authentication token from the provider response.
    ///
    /// Supported token names:
    /// - token
    /// - accessToken
    /// - access_token
    ///
    /// The token may exist either at the root level or inside a "data"
    /// object.
    /// </summary>
    private TokenResult ExtractToken(string body)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new AuthException(
                "Provider auth response is not valid JSON.",
                inner: ex);
        }

        using (document)
        {
            var rootProperties =
                new Dictionary<string, JsonElement>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                rootProperties[property.Name] = property.Value;
            }

            // Handle an explicit provider failure envelope.
            if (rootProperties.TryGetValue(
                    "success",
                    out var success) &&
                success.ValueKind == JsonValueKind.False)
            {
                var message =
                    rootProperties.TryGetValue(
                        "message",
                        out var messageElement)
                        ? messageElement.GetString()
                        : "unknown error";

                throw new AuthException(
                    $"Provider auth rejected: {message}");
            }

            // Check token directly at the root.
            if (TryFindToken(rootProperties) is { } direct)
            {
                return new TokenResult(
                    direct.Value.GetString()!,
                    TryGetExpiresIn(rootProperties));
            }

            // Check token inside a "data" wrapper.
            if (rootProperties.TryGetValue(
                    "data",
                    out var data) &&
                data.ValueKind == JsonValueKind.Object)
            {
                var innerProperties =
                    new Dictionary<string, JsonElement>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var property in data.EnumerateObject())
                {
                    innerProperties[property.Name] = property.Value;
                }

                if (TryFindToken(innerProperties) is { } wrapped)
                {
                    return new TokenResult(
                        wrapped.Value.GetString()!,
                        TryGetExpiresIn(innerProperties));
                }
            }

            _logger.LogError(
                "Auth response had no recognizable token field. Body: {Body}",
                Truncate(body, 500));

            throw new AuthException(
                "Provider auth response did not contain a token field " +
                "(checked token/accessToken/access_token, including a 'data' wrapper).");
        }
    }

    private static KeyValuePair<string, JsonElement>? TryFindToken(
        Dictionary<string, JsonElement> properties)
    {
        foreach (var name in new[]
        {
            "token",
            "accessToken",
            "access_token",
              "result"   // provider's actual field: {"success":true,"message":"token","result":"…"}
        })
        {
            if (properties.TryGetValue(name, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return new KeyValuePair<string, JsonElement>(
                    name,
                    value);
            }
        }

        return null;
    }

    private static int? TryGetExpiresIn(
        Dictionary<string, JsonElement> properties)
    {
        if (properties.TryGetValue(
                "expiresIn",
                out var expiresIn) &&
            expiresIn.TryGetInt32(out var seconds))
        {
            return seconds;
        }

        if (properties.TryGetValue(
                "expires_in",
                out var expiresInSnakeCase) &&
            expiresInSnakeCase.TryGetInt32(out var snakeCaseSeconds))
        {
            return snakeCaseSeconds;
        }

        return null;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "…";
    }

    private sealed record TokenResult(
        string Token,
        int? ExpiresInSec);
}