import { API_BASE_URL } from "./config";

/**
 * GET /api/trades (Phase 5, verified Step 21) — newest-first, paged.
 */
export async function fetchTrades(pageSize = 10) {
  const response = await fetch(`${API_BASE_URL}/api/trades?page=1&pageSize=${pageSize}`);
  if (!response.ok) {
    throw new Error(`Failed to load trades (HTTP ${response.status}).`);
  }
  return response.json();
}