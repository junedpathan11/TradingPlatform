# Assumptions Register — Real-Time Mini Trading Platform

**Last updated:** 2026-08-27 (Phase 2 complete)
**Companion doc:** `docs/api-investigation.md` (raw probe evidence)
**Assignment context:** the assignment (§15) explicitly asks candidates to state assumptions where the auth request format or WebSocket message schema is unclear. This file is that record, maintained throughout the project.

---

## Locked technology stack (context for every assumption)

- **Backend:** ASP.NET Core 8, C#, SignalR, Entity Framework Core 8, SQL Server
- **Frontend:** React + Vite + **JavaScript (not TypeScript)**
- **Testing:** .NET unit tests (xUnit)

---

## A. ASSUMPTIONS (working beliefs, to validate when credentials arrive)

*Note: Phases 1–2 are implemented locally. A1/A2 are implemented in code (AuthService) exactly as stated here — a successful live login remains unverified until real credentials exist.*

| ID | Assumption | Basis | Impact if wrong | Validation plan | Confidence |
|---|---|---|---|---|---|
| A1 | Auth request is `POST` with a JSON body; most likely field names `username` + `password` | Assignment says "REST authentication endpoint"; username/password is the dominant convention for trading-platform auth APIs. **Implemented in AuthService (Phase 2) as `{"username","password"}`** | AuthService request DTO changes (one private record, one file) | On credentials: probe returns 200 → confirmed; if rejected, try candidates — (2) `{"user","password"}` (3) `{"apiKey","apiSecret"}` (4) form-encoded | Medium |
| A2 | Token response is JSON containing the token under a field named `token` or `accessToken` (or `access_token`), possibly under a `data` wrapper; may include `expiresIn`/`expires_in` | Error envelope `{"success","code","message"}` is CONFIRMED (probe); success shape unknown. **AuthService parses all these variants tolerantly (Phase 2)** | Token parsing tweak (isolated in `ExtractToken`) | Capture real success response verbatim during live auth probe | Low-Medium |
| A3 | Token has finite lifetime; WS may drop with 401 when it expires | Typical for trading feeds; confirmed 401-at-handshake behavior for bad tokens | If tokens are long-lived, the expiresIn cache path simply never triggers re-auth (harmless) | During WS probe: note expiry field; observe mid-session 401s | Medium |
| A4 | WS price messages are JSON, one tick per message, containing at minimum `symbol` + `price`; field casing unknown | Assignment examples (`EURUSD`, `1.08348`); JSON is the norm for such feeds | `PriceMessageParser` adjustments (isolated by design — parser is the only component that touches raw messages) | Capture 2–3 raw messages verbatim in WS probe before finalizing the parser | Low-Medium |
| A5 | Symbol universe is FX-style pairs (EURUSD etc.) with ~5-decimal prices | Assignment examples (`EURUSD`, price `1.08348`) | UI formatting only | Read live symbol list from stream (or mirror in mock) | Medium |
| A6 | Plain `http://` / `ws://` (no TLS) is intentional for this exercise and acceptable in the prototype | The assignment specifies these exact URLs | Security posture noted as a known limitation in delivery docs | None needed — document, don't fix | High |
| A7 | Credentials are supplied out-of-band and stored via `dotnet user-secrets` / env vars | Assignment forbids credentials in source (§15). **Confirmed 2026-08-27: the assignment PDF contains no credentials** (user re-verified). Candidate may request credentials from the issuer | None (this is the plan) | User requests credentials from issuer; if supplied → put in secrets.json, probe should return 200 | Confirmed missing / pending issuer |
| A8 | Order execution always uses the in-memory latest price; no order-book/quote depth needed | Assignment §6.2/§7 scope ("use the latest available market price") | None within scope | N/A — assignment-specified | High |

## B. CONFIRMED facts (no longer assumptions — evidence in `docs/api-investigation.md`)

- ✅ Auth endpoint live and reachable (sandbox **and** candidate's local network); returns `401` + JSON envelope `{"success":false,"code":236,"message":"access_denied"}` for all bad/placeholder requests (7 sandbox formats + live .NET HttpClient POST, 2026-08-27).
- ✅ Error envelope shape `{ success: bool, code: int, message: string }`.
- ✅ WS endpoint live; genuine WebSocket upgrade endpoint; validates `token` **query parameter** at handshake; invalid token → `HTTP/1.1 401`, body `null`.
- ✅ Integration sequence: POST auth → token → `ws://…/ws?token={TOKEN}` (per assignment §3, handshake mechanism confirmed by probe).
- ✅ Assignment PDF contains **no provider credentials** (all 5 pages; user-confirmed twice).

## C. UNKNOWN / BLOCKED (requires valid provider credentials)

- ❌ U1 Auth request body schema → AuthService implemented per A1; success path unverified
- ❌ U2 Token success-response shape → tolerant parser covers A2 variants; unverified
- ❌ U3 Token lifetime → cache handles both cases (stated lifetime / none)
- ❌ U4 **Live price message schema** → blocks `PriceMessageParser` finalization (Phase 3/4)
- ❌ U5 Feed rate + symbol universe → blocks throttle tuning
- ❌ U6 Server disconnect/re-auth behavior → blocks reconnect-loop fine-tuning (Phase 3)

**Mitigation for all U-items (assignment-sanctioned):** interfaces at every external boundary (`IAuthService` done; `IPriceFeed` planned) with a **mock implementation** (`MockPriceFeedService`) so all phases can proceed and be demoed; swap/live-verify the moment credentials arrive. **Agreed 2026-08-27: Phase 3 ships dual-mode (`Feed:Mode` = Mock/Live).** Every mock usage must be disclosed in the final delivery's assumptions/limitations list.

## D. Standing design assumptions (architectural, assignment-derived)

- D1: Browser never sees the provider token — backend mediates all provider contact. *(assignment §15; best practice)*
- D2: Single backend process holds the WS connection and latest-price memory store; SQL Server is only for trades.
- D3: "Server time preferred" for trade timestamps → store UTC (`DATETIME2`), display local.
- D4: `Status` limited to `Filled` / `Rejected` (assignment §8); invalid requests get 4xx responses and only executed (Filled) trades are persisted.
- D5: `TradeId` is INT IDENTITY in storage; `TRD10018`-style id is a Phase 5 DTO display format. *(Phase 1)*
- D6: EF provider hardwired to SQL Server via `UseSqlServer` (no provider-switch class — local project has real SQL Server 2022; Sqlite/InMemory packages deliberately skipped). Connection key: `ConnectionStrings:DefaultConnection`. Migrations via VS Package Manager Console. *(Phase 1, local)*
- D7: AuthService implements A1 (`{"username","password"}` JSON POST) + tolerant A2 parsing; provider success shape remains unverified until credentials arrive. Auth failures throw `AuthException` (surfaced as 502 by the temporary probe). *(Phase 2)*
- D8: Provider token cached in memory (lock-protected); cache lifetime = provider-stated `expiresIn` − 60 s safety margin, or indefinite until `InvalidateToken()` when no lifetime is stated. Phase 3's feed calls `InvalidateToken()` on WS 401 to force re-auth. *(Phase 2)*

---
*Last updated: 2026-08-27 — Phase 2 added D7, D8; A1/A2 marked implemented; A7/B updated with credential confirmation.*
*Maintainers: update this file whenever a probe validates/invalidates an assumption. Mark validated items ✅ with the date and evidence link.*