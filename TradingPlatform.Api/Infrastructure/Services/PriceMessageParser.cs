using System.Text.Json;
using TradingPlatform.Api.Models;

namespace TradingPlatform.Api.Infrastructure.Services;

/// <summary>
/// Tolerant parser for provider WS price messages (assignment §9: malformed or
/// unexpected messages must never crash the feed loop).
/// TryParse returns false for anything that is not a usable tick — heartbeats,
/// acks, unknown shapes — so the caller can log-and-skip.
/// Field candidates cover common naming variants; the FINAL mapping is confirmed
/// against raw live captures in Step 15 (docs/assumptions.md A4/U4) and extended here.
/// </summary>
public static class PriceMessageParser
{
    private static readonly string[] SymbolNames = { "symbol", "instrument", "pair", "sym" };
    private static readonly string[] PriceNames = { "price", "last", "lastPrice", "mid", "close" };
    private static readonly string[] BidNames = { "bid", "bidPrice" };
    private static readonly string[] AskNames = { "ask", "askPrice", "offer" };
    private static readonly string[] TimeNames = { "ts", "timestamp", "time", "dateTime" };

    public static bool TryParse(string raw, out PriceTick tick)
    {
        tick = null!;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(raw);
        }
        catch (JsonException)
        {
            return false; // not JSON (heartbeat text, binary frame, garbage) — skip
        }

        using (document)
        {
            // Price feeds frequently arrive as {"data": {...}} envelopes —
            // descend one level when the root holds no symbol itself.
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var props = ToDictionary(root);
            if (!TryGetSymbol(props, out var symbol) && props.TryGetValue("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                props = ToDictionary(data);
                TryGetSymbol(props, out symbol);
            }

            if (string.IsNullOrWhiteSpace(symbol) ||
                !TryGetDecimal(props, PriceNames, out var price) ||
                price <= 0)
            {
                return false; // ack/heartbeat/unknown — skip
            }

            TryGetDecimal(props, BidNames, out var bid);
            TryGetDecimal(props, AskNames, out var ask);
            var receivedUtc = TryGetTimestamp(props) ?? DateTime.UtcNow;

            tick = new PriceTick(symbol.ToUpperInvariant(), price, bid, ask, receivedUtc);
            return true;
        }
    }

    private static Dictionary<string, JsonElement> ToDictionary(JsonElement obj)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in obj.EnumerateObject())
        {
            result[p.Name] = p.Value;
        }
        return result;
    }

    private static bool TryGetSymbol(Dictionary<string, JsonElement> props, out string symbol)
    {
        if (TryFirst(props, SymbolNames, out var v) && v.ValueKind == JsonValueKind.String)
        {
            symbol = v.GetString() ?? string.Empty;
            return true;
        }
        symbol = string.Empty;
        return false;
    }

    private static bool TryGetDecimal(Dictionary<string, JsonElement> props, string[] names, out decimal value)
    {
        value = 0;
        if (!TryFirst(props, names, out var v))
        {
            return false;
        }

        // Providers send numbers as JSON numbers or strings — accept both.
        if (v.ValueKind == JsonValueKind.Number)
        {
            return v.TryGetDecimal(out value);
        }
        return v.ValueKind == JsonValueKind.String &&
               decimal.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                   System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static DateTime? TryGetTimestamp(Dictionary<string, JsonElement> props)
    {
        if (!TryFirst(props, TimeNames, out var v) || v.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var s = v.GetString();
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
        {
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) // feed times are server/UTC (assignment §8)
                : dt.ToUniversalTime();
        }
        return null;
    }

    private static bool TryFirst(Dictionary<string, JsonElement> props, string[] names, out JsonElement value)
    {
        foreach (var name in names)
        {
            if (props.TryGetValue(name, out value))
            {
                return true;
            }
        }
        value = default;
        return false;
    }
}