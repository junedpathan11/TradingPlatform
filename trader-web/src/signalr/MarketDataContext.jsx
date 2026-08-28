import { createContext, useContext, useEffect, useRef, useState } from "react";
import { createMarketConnection } from "./connection";
import { API_BASE_URL } from "../api/config";


const MarketDataContext = createContext(undefined);

// REST fallback so the table can paint before the SignalR handshake
// completes (trading-platform-plan.md Phase 6).
const PRICES_API_URL = `${API_BASE_URL}/api/prices`;
const MANUAL_RETRY_INTERVAL_MS = 5000;

/**
 * Owns the ONE SignalR connection for the whole app (Step 27 — previously
 * useConnectionStatus created its own connection just for the banner; that
 * doesn't scale once other components also need live price events).
 * Exposes both connection status (4 states, same mapping as Step 26) and the
 * latest-tick-per-symbol price map, merged from the initial REST snapshot
 * plus every "prices" SignalR batch.
 */
export function MarketDataProvider({ children }) {
  const [status, setStatus] = useState("Connecting");
  const [prices, setPrices] = useState({});
  const manualRetryTimerRef = useRef(null);
  const stoppedRef = useRef(false);

  useEffect(() => {
    stoppedRef.current = false;

    function mergeTicks(incoming) {
      if (stoppedRef.current || !Array.isArray(incoming) || incoming.length === 0) {
        return;
      }
           setPrices((prev) => {
        const next = { ...prev };
        for (const tick of incoming) {
          // Shallow-merge, not replace: the SignalR "prices" event only
          // carries { symbol, price, changePct, ts } (Phase 4 payload
          // contract) — merging preserves bid/ask from the initial REST
          // snapshot instead of wiping them to undefined on every live tick.
          next[tick.symbol] = { ...next[tick.symbol], ...tick };
        }
        return next;
      });
    }

    // Initial snapshot via REST — non-fatal if it fails, since the hub's own
    // snapshot-on-connect (MarketHub, Phase 4) will populate it regardless.
    fetch(PRICES_API_URL)
      .then((res) => (res.ok ? res.json() : Promise.reject(res.status)))
      .then(mergeTicks)
      .catch(() => {});

    const connection = createMarketConnection();
    connection.on("prices", mergeTicks);

    function scheduleManualRetry() {
      clearTimeout(manualRetryTimerRef.current);
      manualRetryTimerRef.current = setTimeout(() => {
        if (!stoppedRef.current) {
          startConnection();
        }
      }, MANUAL_RETRY_INTERVAL_MS);
    }

    function startConnection() {
      setStatus("Connecting");
      connection
        .start()
        .then(() => {
          if (!stoppedRef.current) setStatus("Connected");
        })
        .catch(() => {
          if (!stoppedRef.current) {
            setStatus("Error");
            scheduleManualRetry();
          }
        });
    }

    connection.onreconnecting(() => {
      if (!stoppedRef.current) setStatus("Connecting");
    });
    connection.onreconnected(() => {
      if (!stoppedRef.current) setStatus("Connected");
    });
    connection.onclose((error) => {
      if (stoppedRef.current) return;
      setStatus(error ? "Error" : "Disconnected");
      scheduleManualRetry();
    });

    startConnection();

    return () => {
      stoppedRef.current = true;
      clearTimeout(manualRetryTimerRef.current);
      connection.stop();
    };
  }, []);

  return (
    <MarketDataContext.Provider value={{ status, prices }}>
      {children}
    </MarketDataContext.Provider>
  );
}

export function useMarketData() {
  const ctx = useContext(MarketDataContext);
  if (!ctx) {
    throw new Error("useMarketData must be used within a MarketDataProvider");
  }
  return ctx;
}