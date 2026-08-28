using TradingPlatform.Api.Infrastructure.Formatting;
using Xunit;

namespace TradingPlatform.Api.Tests;

public class TradeIdFormatterTests
{
    [Theory]
    [InlineData(0, "TRD10000")]
    [InlineData(1, "TRD10001")]
    [InlineData(42, "TRD10042")]
    [InlineData(9999, "TRD19999")]
    public void Format_ReturnsExpectedDisplayId(int tradeId, string expected)
    {
        var result = TradeIdFormatter.Format(tradeId);

        Assert.Equal(expected, result);
    }
}