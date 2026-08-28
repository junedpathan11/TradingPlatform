import { API_BASE_URL } from "./config";

/**
 * GET /api/positions (Phase 5, verified Step 22) — net qty, avg price,
 * realized/unrealized PnL per symbol, computed server-side by
 * PositionCalculator (average-cost, long/short aware).
 */
export async function fetchPositions() {
  const response = await fetch(`${API_BASE_URL}/api/positions`);
  if (!response.ok) {
    throw new Error(`Failed to load positions (HTTP ${response.status}).`);
  }
  return response.json();
}