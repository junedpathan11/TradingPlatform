using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Api.Contracts;
using TradingPlatform.Api.Domain;
using TradingPlatform.Api.Infrastructure.Persistence;
using TradingPlatform.Api.Infrastructure.Services;

namespace TradingPlatform.Api.Controllers;

/// <summary>
/// Net position + realized/unrealized PnL per symbol (assignment §8, Phase 5
/// Step 22 — optional-but-targeted per trading-platform-plan.md). Loads
/// Filled trade history, nets it via IPositionCalculator (pure, no I/O), then
/// merges in the current live price from IPriceStore to compute unrealized PnL.
/// </summary>
[ApiController]
[Route("api/positions")]
public class PositionsController : ControllerBase
{
    private readonly TradingDbContext _db;
    private readonly IPriceStore _priceStore;
    private readonly IPositionCalculator _calculator;

    public PositionsController(TradingDbContext db, IPriceStore priceStore, IPositionCalculator calculator)
    {
        _db = db;
        _priceStore = priceStore;
        _calculator = calculator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var trades = await _db.Trades
            .AsNoTracking()
            .Where(t => t.Status == TradeStatus.Filled)
            .ToListAsync(ct);

        var snapshots = _calculator.Calculate(trades);

        var result = snapshots
            .OrderBy(s => s.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(s =>
            {
                decimal? currentPrice = _priceStore.TryGet(s.Symbol, out var tick) ? tick.Price : null;

                decimal? unrealizedPnL;
                if (s.NetQuantity == 0)
                {
                    unrealizedPnL = 0m;
                }
                else if (currentPrice.HasValue)
                {
                    // s.AvgPrice is guaranteed non-null here: NetQuantity != 0
                    // always implies an open lot exists (see PositionCalculator).
                    unrealizedPnL = s.NetQuantity * (currentPrice.Value - s.AvgPrice!.Value);
                }
                else
                {
                    unrealizedPnL = null;
                }

                return new PositionDto(
                    s.Symbol,
                    s.NetQuantity,
                    s.AvgPrice,
                    currentPrice,
                    unrealizedPnL,
                    s.RealizedPnL);
            })
            .ToList();

        return Ok(result);
    }
}