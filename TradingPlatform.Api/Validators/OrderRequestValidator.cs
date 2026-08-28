using FluentValidation;
using TradingPlatform.Api.Contracts;
using TradingPlatform.Api.Domain;

namespace TradingPlatform.Api.Validators;

/// <summary>
/// FluentValidation rules for POST /api/orders (assignment §6/§8, Phase 5
/// Step 24). Invoked manually in OrdersController (not wired into ASP.NET's
/// automatic ModelState pipeline) so validation failures keep producing the
/// same { error: "..." } response shape already verified in Step 20 — this
/// is a pure internal refactor of the validation, not a behavior change.
/// </summary>
public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public const decimal MaxQuantity = 1000m;

    public OrderRequestValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .WithMessage("Symbol is required.");

        RuleFor(x => x.Side)
            .Must(BeAValidSide)
            .WithMessage("Side must be 'Buy' or 'Sell'.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0m)
            .LessThanOrEqualTo(MaxQuantity)
            .WithMessage($"Quantity must be greater than 0 and at most {MaxQuantity}.");
    }

    private static bool BeAValidSide(string? side) =>
        !string.IsNullOrWhiteSpace(side) &&
        Enum.TryParse<TradeSide>(side, ignoreCase: true, out var parsed) &&
        Enum.IsDefined(typeof(TradeSide), parsed);
}