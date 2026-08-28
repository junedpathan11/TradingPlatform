import { useState,useEffect } from "react";
import Sidebar from "./components/Sidebar";
import Header from "./components/Header";
import PriceTable from "./components/PriceTable";
import OrderTicket from "./components/OrderTicket";
import TradeHistory from "./components/TradeHistory";
import PositionSummary from "./components/PositionSummary";
import { MarketDataProvider, useMarketData } from "./signalr/MarketDataContext";
import { ToastProvider } from "./toast/ToastContext";
import { TradeHistoryProvider } from "./trades/TradeHistoryContext";
import { PositionsProvider } from "./positions/PositionsContext";
import "./App.css";

function AppShell() {
  const { status, prices } = useMarketData();
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => {
    if (!menuOpen) return;
    const handleKeyDown = (e) => {
      if (e.key === 'Escape') setMenuOpen(false);
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [menuOpen]);

  return (
    <div className="app-layout">
      <Sidebar isOpen={menuOpen} onClose={() => setMenuOpen(false)} />
      <div className="app-layout__main">
        <Header status={status} onMenuToggle={() => setMenuOpen((v) => !v)} />
        <main className="app__content">
          <div className="dashboard-grid">
            <PriceTable prices={prices} />
            <OrderTicket />
          </div>
          <div className="dashboard-grid dashboard-grid--secondary">
            <TradeHistory />
            <PositionSummary />
          </div>
        </main>
      </div>
    </div>
  );
}

function App() {
  return (
    <ToastProvider>
      <MarketDataProvider>
        <TradeHistoryProvider>
          <PositionsProvider>
            <AppShell />
          </PositionsProvider>
        </TradeHistoryProvider>
      </MarketDataProvider>
    </ToastProvider>
  );
}

export default App;