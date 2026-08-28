import './Sidebar.css';

const NAV_ITEMS = [
  { key: 'dashboard', icon: '📊', label: 'Dashboard', active: true },
  { key: 'trade-history', icon: '📜', label: 'Trade History', active: false },
  { key: 'positions', icon: '💼', label: 'Positions', active: false },
  { key: 'settings', icon: '⚙️', label: 'Settings', active: false },
];

function Sidebar({ isOpen, onClose }) {
  return (
    <>
      <aside className={`sidebar ${isOpen ? 'sidebar--open' : ''}`}>
        <div className="sidebar__top">
          <div className="sidebar__brand">
            <span className="sidebar__brand-icon">📈</span>
            <span className="sidebar__brand-text">TradeDesk</span>
          </div>
          <button
            type="button"
            className="sidebar__close"
            onClick={onClose}
            aria-label="Close navigation"
          >
            ✕
          </button>
        </div>

        <nav className="sidebar__nav">
          {NAV_ITEMS.map((item) => (
            <a
              key={item.key}
              href="#"
              className={`sidebar__nav-item ${
                item.active ? 'sidebar__nav-item--active' : ''
              }`}
              onClick={(e) => {
                e.preventDefault();
                onClose?.();
              }}
              title={item.label}
            >
              <span className="sidebar__nav-icon">{item.icon}</span>
              <span className="sidebar__nav-label">{item.label}</span>
            </a>
          ))}
        </nav>
      </aside>

      {isOpen && <div className="sidebar-overlay" onClick={onClose} />}
    </>
  );
}

export default Sidebar;