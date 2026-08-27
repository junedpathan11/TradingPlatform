namespace TradingPlatform.Api.Exceptions;

/// <summary>
/// Thrown when provider authentication fails (HTTP error, rejection envelope,
/// or unrecognizable token response). StatusCode carries the provider HTTP
/// status when the failure came from an HTTP response.
/// </summary>
public class AuthException : Exception
{
    public int? StatusCode { get; }

    public AuthException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}