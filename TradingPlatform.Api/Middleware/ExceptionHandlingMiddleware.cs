using System.Net;
using TradingPlatform.Api.Exceptions;

namespace TradingPlatform.Api.Middleware;

/// <summary>
/// Global exception handler (assignment §9, Phase 5 Step 23): catches any
/// unhandled exception from the rest of the pipeline, logs it server-side
/// with the request's TraceIdentifier for correlation, and returns a
/// consistent { error } JSON body — never a raw stack trace to the client.
/// Registered first in Program.cs so it wraps everything downstream
/// (CORS, auth, routing, controllers, SignalR negotiate).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception on {Method} {Path} (traceId={TraceId})",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);

            var statusCode = ex is AuthException authEx && authEx.StatusCode is >= 400 and < 600
                ? authEx.StatusCode.Value
                : (int)HttpStatusCode.InternalServerError;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                traceId = context.TraceIdentifier
            });
        }
    }
}