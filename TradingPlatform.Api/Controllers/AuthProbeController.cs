using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Api.Exceptions;
using TradingPlatform.Api.Infrastructure.Services;

namespace TradingPlatform.Api.Controllers;

/// <summary>TEMPORARY Phase 2 verification endpoint — remove in Phase 5.</summary>
[ApiController]
[Route("api/authprobe")]
public class AuthProbeController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthProbeController(IAuthService authService) => _authService = authService;

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
            // 502 Bad Gateway: the *upstream provider* failed our auth attempt.
            return StatusCode(502, new { result = "auth failed", error = ex.Message });
        }
    }
}