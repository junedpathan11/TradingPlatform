namespace TradingPlatform.Api.Options;

/// <summary>
/// Strongly typed "AuthApi" configuration section.
/// BaseUrl/TokenPath are non-secret (appsettings.json); Username/Password are
/// credentials (user-secrets / env vars — assignment §15, never in source).
/// </summary>
public sealed class AuthApiOptions
{
    public const string SectionName = "AuthApi";

    /// <summary>Provider API root, e.g. "http://s138.acttrader.com:10138" (no trailing slash).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Token endpoint path appended to BaseUrl. Default matches the assignment.</summary>
    public string TokenPath { get; set; } = "/api/v2/auth/token";

    /// <summary>Provider credentials — from user-secrets ONLY (never appsettings.json).</summary>
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}