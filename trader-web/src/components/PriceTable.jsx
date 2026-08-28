import { useMemo } from "react";
import PriceRow from "./PriceRow";
import PriceCard from "./PriceCard";
import "./PriceTable.css";

function sortedTicks(prices) {
  return Object.values(prices).sort((a, b) => a.symbol.localeCompare(b.symbol));
}

/**
 * Live prices panel (Step 27). Renders BOTH a real <table> (desktop/tablet)
 * and a stacked card list (mobile) — CSS media queries decide which is
 * visible, so there's no JS resize logic and no duplicated business logic
 * (both PriceRow and PriceCard share the same usePriceFlash/formatting).
 */
export default function PriceTable({ prices }) {
  const ticks = useMemo(() => sortedTicks(prices), [prices]);

  return (
    <section className="price-table">
      <h2 className="price-table__title">Live Prices</h2>

      {ticks.length === 0 ? (
        <div className="price-table__empty">Waiting for the first price update…</div>
      ) : (
        <>
          <div className="price-table__scroll">
            <table className="price-table__table">
              <thead>
                <tr>
                  <th>Symbol</th>
                  <th>Price</th>
                  <th>Bid</th>
                  <th>Ask</th>
                  <th>Change</th>
                  <th>Updated</th>
                </tr>
              </thead>
              <tbody>
                {ticks.map((tick) => (
                  <PriceRow key={tick.symbol} {...tick} />
                ))}
              </tbody>
            </table>
          </div>

          <div className="price-table__cards">
            {ticks.map((tick) => (
              <PriceCard key={tick.symbol} {...tick} />
            ))}
          </div>
        </>
      )}
    </section>
  );
}