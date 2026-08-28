import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { fetchPositions } from "../api/positions";

const PositionsContext = createContext(undefined);

// Periodic poll so unrealizedPnL visibly drifts with live prices even when
// no new trade has been placed (Step 30 — "recomputed from live prices").
// The backend (PositionCalculator, verified Step 22) remains the single
// source of truth for all the netting/PnL math — no duplicated logic here.
const POLL_INTERVAL_MS = 3000;

export function PositionsProvider({ children }) {
  const [positions, setPositions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);
  const initialLoadDone = useRef(false);

  const refresh = useCallback(() => {
    return fetchPositions()
      .then((data) => {
        setPositions(data);
        setLoadError(null);
      })
      .catch((err) => {
        setLoadError(err.message);
      })
      .finally(() => {
        if (!initialLoadDone.current) {
          initialLoadDone.current = true;
          setLoading(false);
        }
      });
  }, []);

  useEffect(() => {
    refresh();
    const intervalId = setInterval(refresh, POLL_INTERVAL_MS);
    return () => clearInterval(intervalId);
  }, [refresh]);

  return (
    <PositionsContext.Provider value={{ positions, loading, loadError, refresh }}>
      {children}
    </PositionsContext.Provider>
  );
}

export function usePositions() {
  const ctx = useContext(PositionsContext);
  if (!ctx) {
    throw new Error("usePositions must be used within a PositionsProvider");
  }
  return ctx;
}