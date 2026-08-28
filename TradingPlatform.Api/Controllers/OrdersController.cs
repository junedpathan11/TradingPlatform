using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Api.Contracts;
using TradingPlatform.Api.Domain;
using TradingPlatform.Api.Infrastructure.Persistence;
using TradingPlatform.Api.Infrastructure.Services;

namespace TradingPlatform.Api.Controllers;

/// <summary>
/// Order placement (assignment §6/§8, Phase 5): validates the request via
/// FluentValidation (Step 24), executes at the current live price from
/// IPriceStore, persists the trade, and returns a confirmation. Only Filled
/// trades are ever persisted — validation failures return 4xx and are never
/// written (docs/assumptions.md D4).
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly TradingDbContext _db;
    private readonly IPriceStore _priceStore;
    private readonly IValidator<OrderRequest> _validator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        TradingDbContext db,
        IPriceStore priceStore,
        IValidator<OrderRequest> validator,
        ILogger<OrdersController> logger)
    {
        _db = db;
        _priceStore = priceStore;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] OrderRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var message = validationResult.Errors.First().ErrorMessage;
            return BadRequest(new { error = message });
        }

        // Validator already confirmed Side parses to a valid TradeSide.
        Enum.TryParse<TradeSide>(request.Side, ignoreCase: true, out var side);

        var symbol = request.Symbol.Trim().ToUpperInvariant();

        if (!_priceStore.TryGet(symbol, out var tick))
        {
            return Conflict(new { error = $"No live price available for symbol '{symbol}'." });
        }

        var trade = new Trade
        {
            Symbol = symbol,
            Side = side,
            Quantity = request.Quantity,
            Price = tick.Price,
            TimestampUtc = DateTime.UtcNow,
            Status = TradeStatus.Filled
        };

        try
        {
            _db.Trades.Add(trade);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist trade for {Symbol} {Side} {Quantity}.", symbol, side, request.Quantity);
            return StatusCode(500, new { error = "Order could not be saved." });
        }

        var response = new OrderResponse(
            TradeId: $"TRD{10000 + trade.TradeId}",
            Symbol: trade.Symbol,
            Side: trade.Side.ToString(),
            Quantity: trade.Quantity,
            ExecutedPrice: trade.Price,
            Status: trade.Status.ToString(),
            TimestampUtc: trade.TimestampUtc);

        return Ok(response);
    }
}