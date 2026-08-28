# Assumptions Register — Real-Time Mini Trading Platform

**Last updated:** 2026-08-28 (Phase 2.5 — authentication SOLVED & live-verified)
**Companion doc:** `docs/api-investigation.md` (raw probe evidence + §2.4 proven auth spec)
**Assignment context:** the assignment (§15) explicitly asks candidates to state assumptions where the auth request format or WebSocket message schema is unclear. This file is that record, maintained throughout the project.

---

## Locked technology stack (context for every assumption)

- **Backend:** ASP.NET Core 8, C#, SignalR, Entity Framework Core 8, SQL Server
- **Frontend:** React + Vite + **JavaScript (not TypeScript)**
- **Testing:** .NET unit tests (xUnit)

---

## A. ASSUMPTIONS (working beliefs — status updated per the auth resolution)

| ID | Assumption | Status |
|---|---|---|
| A1 | Auth request is `POST` with a JSON body (`username`/`password`) | ❌ **SUPERSEDED (2026-08-28):** auth is HTTP **Digest** challenge-response; the body is `{}` and is never evaluated. See §B and api-investigation §2.4 |
| A2 | Token arrives in a `token`/`accessToken`/`access_token` field (possibly under `data`) | ❌ **SUPERSEDED (2026-08-28):** actual success envelope is `{"success":true,"message":"token","result":"<8-char token>"}` — field is **`result`** (parser now checks all variants incl. `result`) |
| A3 | Token has finite lifetime; WS may drop with 401 on expiry | ⚠️ **PARTIAL:** the response carries **no expiry field** → token treated as valid until server-side invalidation; cache holds until `InvalidateToken()`. Mid-WS-session 401 behavior still unobserved (Phase 3 will reveal) |
| A4 | WS price messages are JSON, one tick per message, min `symbol` + `price`; casing unknown | ⬜ **OPEN — now testable**: first live WS session (Phase 3) settles it. `PriceMessageParser` written tolerantly; raw-message capture planned before finalizing |
| A5 | Symbol universe is FX-style pairs (EURUSD etc.), ~5-decimal prices | ⬜ Open (mock mirrors it; live list read from stream) |
| A6 | Plain `http://`/`ws://` (no TLS) is intentional for this exercise | ✅ Standing (documented as known limitation; not fixable — assignment-specified URLs) |
| A7 | Credentials supplied out-of-band via user-secrets only | ✅ **CLOSED:** HR supplied User ID + Account ID + Password (2026-08-28); stored in `secrets.json` only (`AuthApi:Username` = User ID, `AuthApi:AccountId`, `AuthApi:Password`). Never in source/appsettings/repo |
| A8 | Order execution uses the in-memory latest price; no order-book depth | ✅ Standing (assignment-specified) |
| A9 | ~~Account ID is used to generate an API key~~ (HR statement) | ✅ **RESOLVED:** no key generation step was needed — **Digest identity = the User ID directly**, Digest password = account password. Account ID is NOT part of token issuance; retained in secrets for possible later use (WS subscriptions / trade calls — remaining open question, tracked as U7-lite) |

## B. CONFIRMED facts (evidence in `docs/api-investigation.md`; full spec in its §2.4)

- ✅ Auth endpoint live (sandbox + local); pre-auth rejection: HTTP 401 + `{"success":false,"code":236,"message":"access_denied"}`.
- ✅ **Auth is HTTP Digest** (`WWW-Authenticate: Digest qop="auth", realm="Trade station", stale="FALSE", nonce="…"`; server = AWS/Ada Web Server v3.2.0w).
- ✅ **Digest algorithm = MD5 ONLY.** Any other `algorithm=` token in the Authorization header crashes the server (HTTP 500, Ada `CONSTRAINT_ERROR`, aws-server-http_utils.adb range check). The challenge omits `algorithm=`; .NET's `HttpClientHandler` defaults to SHA-256 in that case → handler-based Digest is unusable → **manual RFC 2617 MD5 handshake implemented in `AuthService`**.
- ✅ **`User-Agent` header REQUIRED at login:** the server stores it into its session table (`SESSN.USER_AGENT`, schema `EFOREX138` — consistent with host `s138`); missing header → `ORA-01400` leaked in a 401 envelope. Fixed via default UA on the auth HttpClient.
- ✅ **Success response (HTTP 200):** `{"success":true,"message":"token","result":"<8-char token>"}`; **no expiry field**. Live-verified 2026-08-28 (`Auth token acquired (length 8, expiresInSec=n/a)`).
- ✅ **Digest identity = User ID** (`csfx…`-style); Digest password = account password. Account ID not used for issuance.
- ✅ WS endpoint live; genuine WebSocket upgrade; validates `token` **query parameter** at handshake; invalid token → HTTP 401, body `null`.
- ✅ Integration sequence confirmed: POST auth (Digest) → token → `ws://…/ws?token={TOKEN}`.
- ✅ Assignment documents (both PDF and text variant) contain no credentials and no auth-schema instructions (forensically verified: text ×3, embedded image, attachments, link annotations, metadata).
- ✅ Internal failure mapping: server-internal login errors (e.g. Oracle) surface as HTTP 401 with `"code":500` inside the envelope.

## C. UNKNOWN / BLOCKED

- ✅ U1 (auth request schema) — **CLOSED 2026-08-28** (Digest, not body).
- ✅ U2 (token success shape) — **CLOSED 2026-08-28** (`result` field).
- ✅ U3 (token lifetime) — **CLOSED 2026-08-28** (no expiry stated; treat as open-ended).
- ⬜ U4 (**WS price message schema**) — last hard unknown; **now testable** with real tokens. Phase 3 Step 15 captures raw messages before parser finalization.
- ⬜ U5 (feed rate + symbol universe) — resolves with the first live session (drives Phase 4 throttle tuning).
- ⬜ U6 (WS disconnect/re-auth behavior) — resolves during Phase 3 soak/reconnect testing (our `InvalidateToken()` hook is the designed answer to mid-session 401s).
- ⬜ U7-lite (Account ID usage) — where/if Account ID appears in WS subscriptions or trade calls; discoverable during Phases 3–5. Account ID kept in `secrets.json` for that purpose.

**Mock fallback still planned** (`Feed:Mode` = Mock/Live): now a *demo-resilience* feature, not a workaround — live is the default and primary path.

## D. Standing design assumptions (architectural)

- D1: Browser never sees the provider token — backend mediates all provider contact. *(assignment §15)*
- D2: Single backend process holds the WS connection and latest-price memory store; SQL Server is only for trades.
- D3: Trade timestamps stored UTC (`DATETIME2`), displayed local ("server time preferred").
- D4: `Status` ∈ {Filled, Rejected}; invalid requests → 4xx; only executed trades persisted.
- D5: `TradeId` = INT IDENTITY in storage; `TRD100xx` display format is a Phase 5 DTO concern.
- D6: EF provider hardwired to SQL Server (`UseSqlServer`); connection key `ConnectionStrings:DefaultConnection`; migrations via VS PMC. *(Phase 1)*
- D7: **AuthService performs the Digest handshake MANUALLY (MD5-only, RFC 2617 qop=auth)** — handler-based Digest is forbidden for this endpoint (crashes it). Raw header via `TryAddWithoutValidation`; `User-Agent` always present; body always `{}`. *(Phase 2.5)*
- D8: Token cached in memory (lock-protected). No expiry is stated by the provider → cache until `InvalidateToken()`; Phase 3's feed calls it on WS 401 to force fresh login. *(Phase 2/2.5)*

---
*Last updated: 2026-08-28 — auth investigation closed (A1/A2 superseded, A3 partial, A7/A9 closed; U1/U2/U3 closed; new confirmed facts in §B).*
*Maintainers: update whenever a probe validates/invalidates an assumption; mark with date + evidence link.*