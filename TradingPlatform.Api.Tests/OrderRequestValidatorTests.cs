using TradingPlatform.Api.Contracts;
using TradingPlatform.Api.Validators;
using Xunit;

namespace TradingPlatform.Api.Tests;

public class OrderRequestValidatorTests
{
    private readonly OrderRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidOrder_Passes()
    {
        var request = new OrderRequest("EURUSD", "Buy", 10m);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("buy")]
    [InlineData("BUY")]
    [InlineData("sell")]
    [InlineData("SELL")]
    public void Validate_SideIsCaseInsensitive_Passes(string side)
    {
        var request = new OrderRequest("EURUSD", side, 1m);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptySymbol_Fails()
    {
        var request = new OrderRequest("", "Buy", 10m);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Symbol is required.");
    }

    [Fact]
    public void Validate_InvalidSide_Fails()
    {
        var request = new OrderRequest("EURUSD", "Hold", 10m);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Side must be 'Buy' or 'Sell'.");
    }

    [Fact]
    public void Validate_ZeroQuantity_Fails()
    {
        var request = new OrderRequest("EURUSD", "Buy", 0m);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NegativeQuantity_Fails()
    {
        var request = new OrderRequest("EURUSD", "Buy", -5m);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_QuantityAtCap_Passes()
    {
        var request = new OrderRequest("EURUSD", "Buy", OrderRequestValidator.MaxQuantity);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_QuantityAboveCap_Fails()
    {
        var request = new OrderRequest("EURUSD", "Buy", OrderRequestValidator.MaxQuantity + 1m);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}