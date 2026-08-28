import { memo } from "react";
import { usePriceFlash } from "../hooks/usePriceFlash";

function formatPrice(value) {
  return value === null || value === undefined ? "—" : Number(value).toFixed(5);
}

function formatChangePct(value) {
  if (value === null || value === undefined) return "—";
  const sign = value > 0 ? "+" : "";
  return `${sign}${Number(value).toFixed(2)}%`;
}

function formatTime(ts) {
  const date = new Date(ts);
  return Number.isNaN(date.getTime()) ? "—" : date.toLocaleTimeString();
}

// React.memo + primitive props (not the tick object) is what makes "only
// changed rows re-render" work: unaffected symbols keep identical prop
// values across renders, so memo bails out for them (Step 27 requirement:
// avoid unnecessary renders during fast updates).
function PriceRow({ symbol, price, bid, ask, changePct, ts }) {
  const { direction, flashKey } = usePriceFlash(price);
  const changeClass =
    changePct > 0 ? "price-table__change--up" : changePct < 0 ? "price-table__change--down" : "";

  return (
    <tr className="price-table__row">
      <td className="price-table__symbol">{symbol}</td>
      <td>
        <span
          key={flashKey}
          className={`price-table__value ${direction ? `price-table__value--${direction}` : ""}`}
        >
          {formatPrice(price)}
        </span>
      </td>
      <td className="price-table__muted">{formatPrice(bid)}</td>
      <td className="price-table__muted">{formatPrice(ask)}</td>
      <td className={changeClass}>{formatChangePct(changePct)}</td>
      <td className="price-table__muted">{formatTime(ts)}</td>
    </tr>
  );
}

export default memo(PriceRow);