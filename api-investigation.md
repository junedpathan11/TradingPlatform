# API Investigation — ActTrader Provider Endpoints

**Project:** Real-Time Mini Trading Platform (candidate assignment)
**Investigation date:** 2026-08-27 (s probes) · 2026-08-27 (live attempt from candidate's local .NET app)
**Method:** `curl` probes from the project sandbox; live POST via the project's `AuthService` (typed HttpClient)
**Status:** Phase 0 pre-credential probe COMPLETE. Live-authenticated success probe PENDING credentials (blocker §5). Local live attempt performed with placeholder credentials → clean 401 (see §2.1).

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

- ✅ **Reachable from the sandbox** (response in ~0.4–0.9 s).
- ✅ **Reachable from the candidate's local/home network** (2026-08-27): live POST from the .NET `AuthService` (typed HttpClient, BaseAddress `http://s138.acttrader.com:10138`, 15 s timeout) with placeholder credentials received **HTTP 401** with the identical envelope — full round trip through the real endpoint confirmed outside the sandbox. Plan risk "firewall blocks unusual ports" cleared for the candidate's environment.
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

All requests were sent with **placeholder credentials** (`test` / `test` or `YOUR_PROVIDER_USERNAME`), since no real credentials exist. The point was to fingerprint the endpoint's error behavior and schema leaks.

| # | Method | Body / Format | Origin | HTTP | Response body |
|---|---|---|---|---|---|
| 1 | POST | `{}` (empty JSON) | sandbox curl | 401 | `{"success":false,"code":236,"message":"access_denied"}` |
| 2 | POST | `{"username":"test","password":"test"}` | sandbox curl | 401 | same |
| 3 | POST | `{"user":"test","password":"test"}` | sandbox curl | 401 | same |
| 4 | POST | `{"apiKey":"test","apiSecret":"test"}` | sandbox curl | 401 | same |
| 5 | POST | `{"grant_type":"password","username":"test","password":"test"}` | sandbox curl | 401 | same |
| 6 | POST | form-encoded `username=test&password=test` | sandbox curl | 401 | same |
| 7 | GET | query string `?username=test&password=test` | sandbox curl | 401 | same |
| 8 | POST | `{"username":"<placeholder>","password":"<placeholder>"}` (user-secrets) | **local .NET AuthService** | 401 | envelope identical (surfaced via /api/authprobe as `Provider auth failed with HTTP 401.`) |

**Conclusions:**
- The endpoint does **not** reveal its expected request schema (identical generic error for all formats — no validation hints, no `WWW-Authenticate` header).
- Real credentials are **required** to progress (see §5).

---

## 4. UNKNOWN / BLOCKED items

| # | Unknown | Why blocked | Unblocks |
|---|---|---|---|
| U1 | Auth request body schema (field names/shape) | Requires valid credentials to distinguish "wrong format" from "wrong credentials" | AuthService request DTO (implemented per A1; confirm/adjust) |
| U2 | Token response shape (field name: `token` / `accessToken` / `access_token`; `data` wrapper; expiry field presence) | Requires successful auth | AuthService response parsing (tolerant parser implemented; confirm) |
| U3 | Token lifetime / expiry semantics | Requires successful auth | Token caching + refresh policy (implemented both-ways; tune) |
| U4 | **Live price message schema** (JSON fields, casing, symbol format, bid/ask vs mid, update rate, payload size) | Requires a successful WS connection | `PriceMessageParser` design (Phase 3/4) |
| U5 | Feed rate (ticks/sec) and symbol universe | Requires live stream | Throttle tuning (broadcast flush interval) |
| U6 | WS server disconnect behavior (idle timeouts, forced drops, re-auth requirements on reconnect) | Requires long-lived connection | Reconnect loop details (Phase 3) |

---

## 5. BLOCKER: missing provider credentials

- The assignment PDF (all 5 pages reviewed) does **not** include any username/password/API key for the provider endpoints. **User re-confirmed 2026-08-27.**
- All live-flow work (U1–U6) is **blocked** until credentials are supplied out-of-band by the candidate/assignment issuer.
- **Action available to candidate:** request credentials from the assignment issuer. If supplied: store in `secrets.json` under `AuthApi:Username` / `AuthApi:Password` (already wired), run `GET /api/authprobe`, expect 200 + token preview; then capture token response + 2–3 raw WS messages per §6 and close U1–U6.
- Credentials must be stored via `dotnet user-secrets` or environment variables — **never** in source code or `appsettings.json` (assignment §15 requirement). User-secrets are already enabled in the project.

---

## 6. Next probe steps (once credentials are available)

1. **Auth probe:** replace placeholders in `secrets.json`; run the app; `GET /api/authprobe` → expect 200. Capture the console's `Auth token acquired (length …, expiresInSec=…)` line and record which field name/wrapper the parser matched → closes U1, U2 (record expiry → U3).
2. **WS probe:** connect with the real token (e.g. `wscat` or the Phase 3 `LivePriceFeedService`); capture **2–3 raw messages verbatim**; note cadence (messages/sec) and symbol list → closes U4, U5.
3. **Disconnect probe:** observe idle timeout / forced-drop behavior; verify whether a 401 mid-session means "token expired → re-auth" (the `InvalidateToken()` path) → closes U6.
4. Update `docs/assumptions.md` (mark A1–A5 validated ✅) and this file (§4 unknowns → confirmed), then continue the phase plan with real schemas.