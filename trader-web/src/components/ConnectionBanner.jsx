import "./ConnectionBanner.css";

const STATUS_LABELS = {
  Connected: "Connected",
  Connecting: "Connecting…",
  Disconnected: "Disconnected",
  Error: "Connection Error",
};

const STATUS_CLASSNAMES = {
  Connected: "connection-banner--connected",
  Connecting: "connection-banner--connecting",
  Disconnected: "connection-banner--disconnected",
  Error: "connection-banner--error",
};

/**
 * Always-visible connection status pill (trading-platform-plan.md Phase 6):
 * top bar pill on desktop, sticky full-width header chip on mobile/tablet.
 */
export default function ConnectionBanner({ status }) {
  const label = STATUS_LABELS[status] ?? status;
  const className = STATUS_CLASSNAMES[status] ?? "";

  return (
    <div className={`connection-banner ${className}`}>
      <span className="connection-banner__dot" />
      <span className="connection-banner__label">{label}</span>
    </div>
  );
}