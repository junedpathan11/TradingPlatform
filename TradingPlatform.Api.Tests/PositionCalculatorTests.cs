using TradingPlatform.Api.Domain;
using TradingPlatform.Api.Infrastructure.Services;
using Xunit;

namespace TradingPlatform.Api.Tests;

public class PositionCalculatorTests
{
    private readonly PositionCalculator _calculator = new();

    private static Trade MakeTrade(
        string symbol, TradeSide side, decimal qty, decimal price,
        DateTime timestampUtc, TradeStatus status = TradeStatus.Filled) => new()
        {
            Symbol = symbol,
            Side = side,
            Quantity = qty,
            Price = price,
            TimestampUtc = timestampUtc,
            Status = status
        };

    [Fact]
    public void Calculate_NoTrades_ReturnsEmptyList()
    {
        var result = _calculator.Calculate(Array.Empty<Trade>());

        Assert.Empty(result);
    }

    [Fact]
    public void Calculate_SingleBuy_ReturnsLongPositionWithNoRealizedPnL()
    {
        var trades = new[]
        {
            MakeTrade("EURUSD", TradeSide.Buy, 10, 1.10m, DateTime.UtcNow)
        };

        var result = _calculator.Calculate(trades);

        var position = Assert.Single(result);
        Assert.Equal("EURUSD", position.Symbol);
        Assert.Equal(10m, position.NetQuantity);
        Assert.Equal(1.10m, position.AvgPrice);
        Assert.Equal(0m, position.RealizedPnL);
    }

    [Fact]
    public void Calculate_BuyThenPartialSell_RealizesPnLAndKeepsRemainder()
    {
        var t0 = DateTime.UtcNow;
        var trades = new[]
        {
            MakeTrade("EURUSD", TradeSide.Buy, 10, 1.00m, t0),
            MakeTrade("EURUSD", TradeSide.Sell, 4, 1.20m, t0.AddMinutes(1))
        };

        var result = _calculator.Calculate(trades);

        var position = Assert.Single(result);
        Assert.Equal(6m, position.NetQuantity);       // 10 - 4 remaining long
        Assert.Equal(1.00m, position.AvgPrice);        // remaining lot's avg cost unchanged
        Assert.Equal(0.80m, position.RealizedPnL);     // 4 * (1.20 - 1.00)
    }

    [Fact]
    public void Calculate_BuyThenFullSell_ReturnsFlatPositionWithNullAvgPrice()
    {
        var t0 = DateTime.UtcNow;
        var trades = new[]
        {
            MakeTrade("EURUSD", TradeSide.Buy, 10, 1.00m, t0),
            MakeTrade("EURUSD", TradeSide.Sell, 10, 1.05m, t0.AddMinutes(1))
        };

        var result = _calculator.Calculate(trades);

        var position = Assert.Single(result);
        Assert.Equal(0m, position.NetQuantity);
        Assert.Null(position.AvgPrice);
        Assert.Equal(0.50m, position.RealizedPnL);     // 10 * (1.05 - 1.00)
    }

    [Fact]
    public void Calculate_ShortSelling_NetsNegativeQuantityWithAvgPrice()
    {
        var trades = new[]
        {
            MakeTrade("XAUUSD", TradeSide.Sell, 5, 2340m, DateTime.UtcNow)
        };

        var result = _calculator.Calculate(trades);

        var position = Assert.Single(result);
        Assert.Equal(-5m, position.NetQuantity);
        Assert.Equal(2340m, position.AvgPrice);
        Assert.Equal(0m, position.RealizedPnL);
    }

    [Fact]
    public void Calculate_PositionFlip_LongToShort_RealizesPnLAndOpensOppositeSide()
    {
        var t0 = DateTime.UtcNow;
        var trades = new[]
        {
            MakeTrade("BTCUSD", TradeSide.Buy, 5, 60000m, t0),
            MakeTrade("BTCUSD", TradeSide.Sell, 8, 61000m, t0.AddMinutes(1))
        };

        var result = _calculator.Calculate(trades);

        var position = Assert.Single(result);
        Assert.Equal(-3m, position.NetQuantity);       // flipped from +5 to -3
        Assert.Equal(61000m, position.AvgPrice);        // new short lot priced at the flip trade
        Assert.Equal(5000m, position.RealizedPnL);      // closed 5 @ (61000-60000) = 5000
    }

    [Fact]
    public void Calculate_MultipleSymbols_GroupsIndependently()
    {
        var t0 = DateTime.UtcNow;
        var trades = new[]
        {
            MakeTrade("EURUSD", TradeSide.Buy, 10, 1.00m, t0),
            MakeTrade("GBPUSD", TradeSide.Sell, 3, 1.27m, t0.AddMinutes(1))
        };

        var result = _calculator.Calculate(trades);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Symbol == "EURUSD" && p.NetQuantity == 10m);
        Assert.Contains(result, p => p.Symbol == "GBPUSD" && p.NetQuantity == -3m);
    }

    [Fact]
    public void Calculate_IgnoresNonFilledTrades()
    {
        var trades = new[]
        {
            MakeTrade("EURUSD", TradeSide.Buy, 10, 1.00m, DateTime.UtcNow, TradeStatus.Rejected)
        };

        var result = _calculator.Calculate(trades);

        Assert.Empty(result);
    }
}