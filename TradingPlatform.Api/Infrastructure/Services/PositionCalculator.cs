using TradingPlatform.Api.Domain;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <inheritdoc cref="IPositionCalculator"/>
public sealed class PositionCalculator : IPositionCalculator
{
    public IReadOnlyList<PositionSnapshot> Calculate(IEnumerable<Trade> trades)
    {
        var results = new List<PositionSnapshot>();

        var bySymbol = trades
            .Where(t => t.Status == TradeStatus.Filled)
            .GroupBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase);

        foreach (var group in bySymbol)
        {
            decimal netQuantity = 0;
            decimal avgCost = 0;
            decimal realizedPnL = 0;

            foreach (var trade in group.OrderBy(t => t.TimestampUtc))
            {
                var delta = trade.Side == TradeSide.Buy ? trade.Quantity : -trade.Quantity;
                if (delta == 0)
                {
                    continue;
                }

                var sameDirection = netQuantity == 0 || Math.Sign(netQuantity) == Math.Sign(delta);

                if (sameDirection)
                {
                    // Opening or adding to a position in the same direction:
                    // roll the weighted average cost forward.
                    var existingAbs = Math.Abs(netQuantity);
                    var addedAbs = Math.Abs(delta);
                    avgCost = existingAbs == 0
                        ? trade.Price
                        : (existingAbs * avgCost + addedAbs * trade.Price) / (existingAbs + addedAbs);
                    netQuantity += delta;
                    continue;
                }

                // Opposite direction: this trade closes some/all of the open
                // position, and may flip to the other side if it overshoots.
                var closingAbs = Math.Min(Math.Abs(delta), Math.Abs(netQuantity));
                var pnlPerUnit = netQuantity > 0
                    ? trade.Price - avgCost   // was long, closing via a Sell
                    : avgCost - trade.Price;  // was short, closing via a Buy
                realizedPnL += closingAbs * pnlPerUnit;

                var remainderAbs = Math.Abs(delta) - closingAbs;
                netQuantity += delta;

                if (remainderAbs > 0)
                {
                    // Flipped through flat and opened a new position on the
                    // other side, priced at this same trade's execution price.
                    avgCost = trade.Price;
                }
                else if (netQuantity == 0)
                {
                    avgCost = 0; // fully closed — no open lot left
                }
                // else: position reduced but the same side remains open —
                // avgCost of the remaining lot is unchanged.
            }

            results.Add(new PositionSnapshot(
                group.Key,
                netQuantity,
                netQuantity == 0 ? null : avgCost,
                realizedPnL));
        }

        return results;
    }
}