using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Api.Contracts;
using TradingPlatform.Api.Infrastructure.Persistence;

namespace TradingPlatform.Api.Controllers;

/// <summary>
/// Recent trade history (assignment §8, Phase 5 Step 21): reads persisted
/// trades from dbo.Trades, newest first, paged. Reuses TradingDbContext
/// directly — no repository layer, matching OrdersController's convention
/// (Step 20) since this project has no service/repository pattern yet.
/// </summary>
[ApiController]
[Route("api/trades")]
public class TradesController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly TradingDbContext _db;

    public TradesController(TradingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            pageSize = DefaultPageSize;
        }

        var trades = await _db.Trades
            .AsNoTracking()
            .OrderByDescending(t => t.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TradeDto(
                $"TRD{10000 + t.TradeId}",
                t.Symbol,
                t.Side.ToString(),
                t.Quantity,
                t.Price,
                t.Status.ToString(),
                t.TimestampUtc))
            .ToListAsync(ct);

        return Ok(trades);
    }
}