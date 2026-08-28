import ConnectionBanner from "./ConnectionBanner";
import "./Header.css";

export default function Header({ status, onMenuToggle }) {
  return (
    <header className="app-header">
      <button
        type="button"
        className="app-header__menu-btn"
        onClick={onMenuToggle}
        aria-label="Toggle navigation menu"
      >
        ☰
      </button>
      <h1 className="app-header__title">Real-Time Mini Trading Platform</h1>
      <ConnectionBanner status={status} />
    </header>
  );
}