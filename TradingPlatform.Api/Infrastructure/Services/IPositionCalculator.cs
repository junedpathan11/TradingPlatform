using TradingPlatform.Api.Domain;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Pure, stateless per-symbol position/PnL calculator (Phase 5 Step 22,
/// trading-platform-plan.md Phase 7 PositionSummary model). Takes trade
/// history in, returns netted positions out — no I/O, no live-price
/// dependency, so it's independently unit-testable (Phase 9 checklist).
/// </summary>
public interface IPositionCalculator
{
    /// <summary>
    /// Nets Filled trades per symbol using average-cost accounting (supports
    /// long and short). Non-Filled trades are ignored defensively even though
    /// only Filled trades are ever persisted today (docs/assumptions.md D4).
    /// </summary>
    IReadOnlyList<PositionSnapshot> Calculate(IEnumerable<Trade> trades);
}

/// <summary>
/// One symbol's netted position. AvgPrice is null when NetQuantity is 0 —
/// there is no open lot to average. RealizedPnL always has a value (0 if
/// nothing has ever closed).
/// </summary>
public sealed record PositionSnapshot(
    string Symbol,
    decimal NetQuantity,
    decimal? AvgPrice,
    decimal RealizedPnL);