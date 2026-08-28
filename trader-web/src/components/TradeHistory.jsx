import { useTradeHistory } from "../trades/TradeHistoryContext";
import "./TradeHistory.css";

function formatTime(ts) {
  const date = new Date(ts);
  return Number.isNaN(date.getTime()) ? "—" : date.toLocaleString();
}

export default function TradeHistory() {
  const { trades, loading, loadError } = useTradeHistory();

  return (
    <section className="trade-history">
      <h2 className="trade-history__title">Trade History</h2>

      {loading ? (
        <div className="trade-history__empty">Loading trades…</div>
      ) : loadError ? (
        <div className="trade-history__error">{loadError}</div>
      ) : trades.length === 0 ? (
        <div className="trade-history__empty">No trades yet.</div>
      ) : (
        <>
          <div className="trade-history__scroll">
            <table className="trade-history__table">
              <thead>
                <tr>
                  <th>Trade ID</th>
                  <th>Symbol</th>
                  <th>Side</th>
                  <th>Qty</th>
                  <th>Price</th>
                  <th>Status</th>
                  <th>Time</th>
                </tr>
              </thead>
              <tbody>
                {trades.map((t) => (
                  <tr key={t.tradeId}>
                    <td className="trade-history__id">{t.tradeId}</td>
                    <td>{t.symbol}</td>
                    <td className={t.side === "Buy" ? "trade-history__side--buy" : "trade-history__side--sell"}>
                      {t.side}
                    </td>
                    <td>{t.quantity}</td>
                    <td>{Number(t.price).toFixed(5)}</td>
                    <td>{t.status}</td>
                    <td className="trade-history__muted">{formatTime(t.timestampUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="trade-history__cards">
            {trades.map((t) => (
              <div className="trade-card" key={t.tradeId}>
                <div className="trade-card__top">
                  <span className="trade-card__id">{t.tradeId}</span>
                  <span
                    className={t.side === "Buy" ? "trade-history__side--buy" : "trade-history__side--sell"}
                  >
                    {t.side}
                  </span>
                </div>
                <div className="trade-card__mid">
                  <span className="trade-card__symbol">{t.symbol}</span>
                  <span>{t.quantity} @ {Number(t.price).toFixed(5)}</span>
                </div>
                <div className="trade-card__bottom">
                  <span>{t.status}</span>
                  <span className="trade-history__muted">{formatTime(t.timestampUtc)}</span>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </section>
  );
}