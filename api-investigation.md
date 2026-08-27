# API Investigation — ActTrader Provider Endpoints

**Project:** Real-Time Mini Trading Platform (candidate assignment)
**Investigation date:** 2026-08-27
**Method:** `curl` probes from the project sandbox (no credentials available yet — see §5)
**Status:** Phase 0 pre-credential probe COMPLETE. Live-authenticated probe PENDING credentials.

---

## 1. Endpoints under investigation (from assignment PDF §3)

| Item | Value |
|---|---|
| REST auth endpoint | `http://s138.acttrader.com:10138/api/v2/auth/token` |
| WebSocket endpoint | `ws://s138.acttrader.com:22138/ws?token={TOKEN}` |
| Documented sequence | POST to auth endpoint → receive token → connect WS with `{TOKEN}` replaced |

---

## 2. CONFIRMED findings

### 2.1 REST auth endpoint

- ✅ **Reachable** from the sandbox (response in ~0.4–0.9 s).
- ✅ **Responds with JSON** on failure. Confirmed error envelope:

  ```json
  {"success": false, "code": 236, "message": "access_denied"}
  ```

  - HTTP status: **401 Unauthorized**
  - Envelope shape `{ success: bool, code: int, message: string }` is CONFIRMED for error responses.
  - `code: 236` / `message: "access_denied"` observed for **every** unauthenticated/incorrect request (see §3) — this appears to be the generic "bad credentials or bad request" response.
- ✅ Endpoint is alive and content-type-aware; it does **not** 404 or time out.

### 2.2 WebSocket endpoint

- ✅ **Reachable** from the sandbox (~0.5 s handshake rejection).
- ✅ It is a genuine WebSocket upgrade endpoint (HTTP/1.1, honors `Upgrade: websocket` / `Sec-WebSocket-*` headers).
- ✅ **Token is validated at handshake time via the `token` query parameter.** An invalid token is rejected before the socket upgrades:

  ```
  HTTP/1.1 401 Unauthorized
  Content-Type: application/json
  Content-Length: 5

  null
  ```

  (Raw response body is the JSON literal `null`.)
- ✅ This confirms the documented `?token={TOKEN}` query-param mechanism: invalid token → 401 at upgrade; valid token → (presumably) `101 Switching Protocols`. Valid-token behavior is **unverified until credentials exist**.

---

## 3. Tested request formats and results (auth endpoint)

All requests were sent with **placeholder credentials** (`test` / `test`), since no real credentials exist yet. The point was to fingerprint the endpoint's error behavior and schema leaks.

| # | Method | Body / Format | HTTP | Response body |
|---|---|---|---|---|
| 1 | POST | `{}` (empty JSON) | 401 | `{"success":false,"code":236,"message":"access_denied"}` |
| 2 | POST | `{"username":"test","password":"test"}` | 401 | same |
| 3 | POST | `{"user":"test","password":"test"}` | 401 | same |
| 4 | POST | `{"apiKey":"test","apiSecret":"test"}` | 401 | same |
| 5 | POST | `{"grant_type":"password","username":"test","password":"test"}` | 401 | same |
| 6 | POST | form-encoded `username=test&password=test` | 401 | same |
| 7 | GET | query string `?username=test&password=test` | 401 | same |

**Conclusions:**
- The endpoint does **not** reveal its expected request schema (identical generic error for all formats — no validation hints, no `WWW-Authenticate` header).
- Real credentials are **required** to progress (see §5).

---

## 4. UNKNOWN / BLOCKED items

| # | Unknown | Why blocked | Unblocks |
|---|---|---|---|
| U1 | Auth request body schema (field names/shape) | Requires valid credentials to distinguish "wrong format" from "wrong credentials" | AuthService request DTO (Phase 2) |
| U2 | Token response shape (field name: `token` / `accessToken` / envelope-wrapped; expiry field presence) | Requires successful auth | AuthService response parsing (Phase 2) |
| U3 | Token lifetime / expiry semantics | Requires successful auth | Token caching + refresh policy (Phase 2/3) |
| U4 | **Live price message schema** (JSON fields, casing, symbol format, bid/ask vs mid, update rate, payload size) | Requires a successful WS connection | `PriceMessageParser` design (Phase 3/4) |
| U5 | Feed rate (ticks/sec) and symbol universe | Requires live stream | Throttle tuning (broadcast flush interval) |
| U6 | WS server disconnect behavior (idle timeouts, forced drops, re-auth requirements on reconnect) | Requires long-lived connection | Reconnect loop details (Phase 3) |

---

## 5. BLOCKER: missing provider credentials

- The assignment PDF (all 5 pages reviewed) does **not** include any username/password/API key for the provider endpoints.
- All live-flow work (U1–U6) is **blocked** until credentials are supplied out-of-band by the candidate/assignment issuer.
- Credentials must be stored via `dotnet user-secrets` or environment variables — **never** in source code or `appsettings.json` (assignment §15 requirement).

---

## 6. Next probe steps (once credentials are available)

1. **Auth probe:** POST the credential formats in priority order (see `docs/assumptions.md` A1) until `success:true`; capture the full token response JSON verbatim; record HTTP status and headers.
2. **WS probe:** connect with the real token (e.g. `wscat` or a small C#/Node script); capture **2–3 raw messages verbatim**; note cadence (messages/sec) and symbol list.
3. **Disconnect probe:** observe idle timeout / forced-drop behavior; verify whether a 401 mid-session means "token expired → re-auth".
4. Update `docs/assumptions.md` (mark validated assumptions) and this file (§4 unknowns → confirmed), then proceed to Phase 2/3 design with real schemas.