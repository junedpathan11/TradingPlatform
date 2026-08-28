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

// Mobile-width counterpart to PriceRow — same data, stacked-card presentation
// instead of a table row (assignment: compact cards/stacked layout on mobile).
function PriceCard({ symbol, price, bid, ask, changePct, ts }) {
  const { direction, flashKey } = usePriceFlash(price);
  const changeClass =
    changePct > 0 ? "price-card__change--up" : changePct < 0 ? "price-card__change--down" : "";

  return (
    <div className="price-card">
      <div className="price-card__top">
        <span className="price-card__symbol">{symbol}</span>
        <span
          key={flashKey}
          className={`price-card__value ${direction ? `price-card__value--${direction}` : ""}`}
        >
          {formatPrice(price)}
        </span>
      </div>
      <div className="price-card__bottom">
        <span className="price-card__muted">Bid {formatPrice(bid)}</span>
        <span className="price-card__muted">Ask {formatPrice(ask)}</span>
        <span className={changeClass}>{formatChangePct(changePct)}</span>
      </div>
      <div className="price-card__time">{formatTime(ts)}</div>
    </div>
  );
}

export default memo(PriceCard);