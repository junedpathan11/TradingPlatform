import { usePositions } from "../positions/PositionsContext";
import "./PositionSummary.css";

function formatNumber(value, digits = 5) {
  return value === null || value === undefined ? "—" : Number(value).toFixed(digits);
}

function formatPnL(value) {
  if (value === null || value === undefined) return "—";
  const sign = value > 0 ? "+" : "";
  return `${sign}${Number(value).toFixed(2)}`;
}

function pnlClass(value) {
  if (value === null || value === undefined || value === 0) return "";
  return value > 0 ? "positions__pnl--up" : "positions__pnl--down";
}

export default function PositionSummary() {
  const { positions, loading, loadError } = usePositions();

  return (
    <section className="positions">
      <h2 className="positions__title">Positions</h2>

      {loading ? (
        <div className="positions__empty">Loading positions…</div>
      ) : loadError ? (
        <div className="positions__error">{loadError}</div>
      ) : positions.length === 0 ? (
        <div className="positions__empty">No positions yet.</div>
      ) : (
        <>
          <div className="positions__scroll">
            <table className="positions__table">
              <thead>
                <tr>
                  <th>Symbol</th>
                  <th>Net Qty</th>
                  <th>Avg Price</th>
                  <th>Current Price</th>
                  <th>Unrealized PnL</th>
                  <th>Realized PnL</th>
                </tr>
              </thead>
              <tbody>
                {positions.map((p) => (
                  <tr key={p.symbol}>
                    <td className="positions__symbol">{p.symbol}</td>
                    <td className={p.netQuantity > 0 ? "positions__side--long" : p.netQuantity < 0 ? "positions__side--short" : ""}>
                      {p.netQuantity}
                    </td>
                    <td>{formatNumber(p.avgPrice)}</td>
                    <td>{formatNumber(p.currentPrice)}</td>
                    <td className={pnlClass(p.unrealizedPnL)}>{formatPnL(p.unrealizedPnL)}</td>
                    <td className={pnlClass(p.realizedPnL)}>{formatPnL(p.realizedPnL)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="positions__cards">
            {positions.map((p) => (
              <div className="position-card" key={p.symbol}>
                <div className="position-card__top">
                  <span className="position-card__symbol">{p.symbol}</span>
                  <span className={p.netQuantity > 0 ? "positions__side--long" : p.netQuantity < 0 ? "positions__side--short" : ""}>
                    Net {p.netQuantity}
                  </span>
                </div>
                <div className="position-card__row">
                  <span className="positions__muted">Avg {formatNumber(p.avgPrice)}</span>
                  <span className="positions__muted">Cur {formatNumber(p.currentPrice)}</span>
                </div>
                <div className="position-card__row">
                  <span className={pnlClass(p.unrealizedPnL)}>Unreal {formatPnL(p.unrealizedPnL)}</span>
                  <span className={pnlClass(p.realizedPnL)}>Real {formatPnL(p.realizedPnL)}</span>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </section>
  );
}