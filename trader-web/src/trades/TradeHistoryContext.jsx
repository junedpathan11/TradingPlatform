import { createContext, useContext, useEffect, useState } from "react";
import { fetchTrades } from "../api/trades";

const TradeHistoryContext = createContext(undefined);
const PAGE_SIZE = 10;

/**
 * Shared trade-history state (Step 29). OrderTicket calls addTrade() right
 * after a successful POST /api/orders so the new trade appears immediately
 * — no re-fetch, no page reload (trading-platform-plan.md Phase 7).
 */
export function TradeHistoryProvider({ children }) {
  const [trades, setTrades] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    fetchTrades(PAGE_SIZE)
      .then((data) => {
        if (!cancelled) setTrades(data);
      })
      .catch((err) => {
        if (!cancelled) setLoadError(err.message);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  function addTrade(trade) {
    // Map the POST /api/orders response shape onto the same shape
    // GET /api/trades uses, so TradeRow doesn't need to know which source
    // a given item came from.
    setTrades((prev) =>
      [
        {
          tradeId: trade.tradeId,
          symbol: trade.symbol,
          side: trade.side,
          quantity: trade.quantity,
          price: trade.executedPrice,
          status: trade.status,
          timestampUtc: trade.timestampUtc,
        },
        ...prev,
      ].slice(0, PAGE_SIZE)
    );
  }

  return (
    <TradeHistoryContext.Provider value={{ trades, loading, loadError, addTrade }}>
      {children}
    </TradeHistoryContext.Provider>
  );
}

export function useTradeHistory() {
  const ctx = useContext(TradeHistoryContext);
  if (!ctx) {
    throw new Error("useTradeHistory must be used within a TradeHistoryProvider");
  }
  return ctx;
}