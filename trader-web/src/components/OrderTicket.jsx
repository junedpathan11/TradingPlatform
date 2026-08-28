import { useEffect, useState } from "react";
import { useMarketData } from "../signalr/MarketDataContext";
import { placeOrder } from "../api/orders";
import { useToast } from "../toast/ToastContext";
import { useTradeHistory } from "../trades/TradeHistoryContext";
import { usePositions } from "../positions/PositionsContext";
import "./OrderTicket.css";

// Mirrors OrderRequestValidator.MaxQuantity (backend, Step 24) — a UX nicety
// only; the backend remains the actual source of truth/enforcement.
const MAX_QUANTITY = 1000;

export default function OrderTicket() {
  const { prices } = useMarketData();
  const { showToast } = useToast();
  const { addTrade } = useTradeHistory();
   const { refresh: refreshPositions } = usePositions();

  const symbols = Object.keys(prices).sort();

  const [symbol, setSymbol] = useState("");
  const [side, setSide] = useState("Buy");
  const [quantity, setQuantity] = useState("1");
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState(null);

  // Default to the first available symbol once prices arrive.
  useEffect(() => {
    if (!symbol && symbols.length > 0) {
      setSymbol(symbols[0]);
    }
  }, [symbols, symbol]);

  const currentPrice = prices[symbol]?.price;

  async function handleSubmit(event) {
    event.preventDefault();
    setFormError(null);

    const qtyNumber = Number(quantity);
    if (!symbol) {
      setFormError("Select a symbol.");
      return;
    }
    if (!Number.isFinite(qtyNumber) || qtyNumber <= 0) {
      setFormError("Quantity must be greater than 0.");
      return;
    }
    if (qtyNumber > MAX_QUANTITY) {
      setFormError(`Quantity must be at most ${MAX_QUANTITY}.`);
      return;
    }

    setSubmitting(true);
    try {
      const result = await placeOrder({ symbol, side, quantity: qtyNumber });
       addTrade(result);
         refreshPositions();
      showToast(
        "success",
        `${result.side} ${result.quantity} ${result.symbol} filled @ ${result.executedPrice} — ${result.tradeId}`
      );
    } catch (err) {
      showToast("error", err.message || "Order could not be placed.");
    } finally {
      setSubmitting(false);
    }
  }

  if (symbols.length === 0) {
    return (
      <section className="order-ticket">
        <h2 className="order-ticket__title">Quick Trade</h2>
        <div className="order-ticket__empty">Waiting for live prices…</div>
      </section>
    );
  }

  return (
    <section className="order-ticket">
      <h2 className="order-ticket__title">Quick Trade</h2>

      <form className="order-ticket__form" onSubmit={handleSubmit}>
        <label className="order-ticket__field">
          <span>Symbol</span>
          <select value={symbol} onChange={(e) => setSymbol(e.target.value)}>
            {symbols.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>

        <div className="order-ticket__side">
          <button
            type="button"
            className={`order-ticket__side-btn order-ticket__side-btn--buy ${
              side === "Buy" ? "is-active" : ""
            }`}
            onClick={() => setSide("Buy")}
          >
            Buy
          </button>
          <button
            type="button"
            className={`order-ticket__side-btn order-ticket__side-btn--sell ${
              side === "Sell" ? "is-active" : ""
            }`}
            onClick={() => setSide("Sell")}
          >
            Sell
          </button>
        </div>

        <label className="order-ticket__field">
          <span>Quantity</span>
          <input
            type="number"
            min="0"
            step="any"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
          />
        </label>

        <div className="order-ticket__price">
          <span>Current Price</span>
          <strong>{currentPrice !== undefined ? Number(currentPrice).toFixed(5) : "—"}</strong>
        </div>

        {formError && <div className="order-ticket__error">{formError}</div>}

        <button
          type="submit"
          className={`order-ticket__submit order-ticket__submit--${side.toLowerCase()}`}
          disabled={submitting}
        >
          {submitting ? "Placing…" : `${side} ${symbol}`}
        </button>
      </form>
    </section>
  );
}