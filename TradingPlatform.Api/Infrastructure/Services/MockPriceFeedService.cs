using TradingPlatform.Api.Models;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Demo fallback feed (Feed:Mode = "Mock"): random-walk ticks for the reference
/// mockup's instruments, written into the SAME IPriceStore pipeline as the live
/// feed. Emitting a visible warning log so a demo never mistakes synthetic data
/// for real market data. Disclosed in final delivery docs.
/// </summary>
public class MockPriceFeedService : BackgroundService
{
    private static readonly (string Symbol, decimal Base)[] Instruments =
    {
        ("EURUSD", 1.08348m),
        ("GBPUSD", 1.27214m),
        ("USDJPY", 156.248m),
        ("XAUUSD", 2334.62m),
        ("UOIL", 77.375m),
        ("BTCUSD", 67670.30m),
    };

    private readonly IPriceStore _priceStore;
    private readonly FeedStateService _feedState;
    private readonly ILogger<MockPriceFeedService> _logger;

    public MockPriceFeedService(
        IPriceStore priceStore,
        FeedStateService feedState,
        ILogger<MockPriceFeedService> logger)
    {
        _priceStore = priceStore;
        _feedState = feedState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _feedState.SetState(FeedConnectionState.Connected);
        _logger.LogWarning("MOCK FEED ACTIVE (Feed:Mode=Mock) — synthetic ticks, NOT real market data.");

        var prices = Instruments.ToDictionary(i => i.Symbol, i => i.Base);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var (symbol, _) in Instruments)
            {
                // Random walk: ±0.02% per tick, mid price with a small bid/ask spread.
                var drift = (decimal)(Random.Shared.NextDouble() - 0.5) * 0.0004m;
                var mid = Math.Round(prices[symbol] * (1 + drift), 5);
                prices[symbol] = mid;

                var halfSpread = Math.Round(mid * 0.00005m, 5);
                _priceStore.Update(new PriceTick(
                    symbol, mid, mid - halfSpread, mid + halfSpread, DateTime.UtcNow));
            }

            try
            {
                await Task.Delay(300, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _feedState.SetState(FeedConnectionState.Disconnected);
    }
}