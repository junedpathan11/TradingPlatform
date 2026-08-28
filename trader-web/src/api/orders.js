import { API_BASE_URL } from "./config";

/**
 * POST /api/orders (Phase 5, verified Steps 20/24). Throws with the
 * backend's own { error: "..." } message on any non-2xx response so the
 * caller can show it directly — no guessing/inventing error text.
 */
export async function placeOrder({ symbol, side, quantity }) {
  const response = await fetch(`${API_BASE_URL}/api/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ symbol, side, quantity }),
  });

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    throw new Error(body?.error || `Order failed (HTTP ${response.status}).`);
  }

  return body;
}