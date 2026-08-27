# Assumptions Register — Real-Time Mini Trading Platform

**Last updated:** 2026-08-27 (Phase 1 complete)
**Companion doc:** `docs/api-investigation.md` (raw probe evidence)
**Assignment context:** the assignment (§15) explicitly asks candidates to state assumptions where the auth request format or WebSocket message schema is unclear. This file is that record, maintained throughout the project.

---

## Locked technology stack (context for every assumption)

- **Backend:** ASP.NET Core 8, C#, SignalR, Entity Framework Core, SQL Server
- **Frontend:** React + Vite + **JavaScript (not TypeScript)** — existing TS scaffold pending separate conversion
- **Testing:** .NET unit tests (xUnit)

---

## A. ASSUMPTIONS (working beliefs, to validate when credentials arrive)

*Note: Phase 1 (config + data layer) is complete — architecture/design assumptions D1–D6 below are now part of the codebase.*

| ID | Assumption | Basis | Impact if wrong | Validation plan | Confidence |
|---|---|---|---|---|---|
| A1 | Auth request is `POST` with a JSON body; most likely field names `username` + `password` | Assignment says "REST authentication endpoint"; username/password is the dominant convention for trading-platform auth APIs | AuthService request DTO changes (small, isolated) | On credentials: try candidate bodies in order — (1) `{"username","password"}` (2) `{"user","password"}` (3) `{"apiKey","apiSecret"}` (4) form-encoded. First `success:true` wins; record verbatim | Medium |
| A2 | Token response is JSON containing the token under a field named `token` or `accessToken`, likely wrapped in the confirmed `{"success":…,"code":…,"message":…}` envelope; may include an expiry field | Error envelope `{"success","code","message"}` is CONFIRMED (probe); success shape unknown | Token parsing logic (tiny — tolerant parser handles all variants) | Capture real success response verbatim during auth probe | Low-Medium |
| A3 | Token has finite lifetime; WS may drop with 401 when it expires | Typical for trading feeds; confirmed 401-at-handshake behavior for bad tokens | If tokens are long-lived, our re-auth-on-401 logic simply never fires (harmless) | During WS probe: note any expiry field; observe mid-session 401s | Medium |
| A4 | WS price messages are JSON, one tick per message, containing at minimum `symbol` + `price`; field casing unknown | Assignment examples (`EURUSD`, `1.08348`); JSON is the norm for such feeds | `PriceMessageParser` adjustments (isolated by design — parser is the only component that touches raw messages) | Capture 2–3 raw messages verbatim in WS probe before writing the parser | Low-Medium |
| A5 | Symbol universe is FX-style pairs (EURUSD etc.) with ~5-decimal prices | Assignment examples (`EURUSD`, price `1.08348`) | UI formatting only | Read live symbol list from stream | Medium |
| A6 | Plain `http://` / `ws://` (no TLS) is intentional for this exercise and acceptable in the prototype | The assignment specifies these exact URLs | Security posture noted as a known limitation in delivery docs | None needed — document, don't fix | High |
| A7 | Credentials will be supplied out-of-band and stored via `dotnet user-secrets` / env vars | Assignment forbids credentials in source (§15); PDF contains none | None (this is the plan) | User supplies credentials | Pending user |
| A8 | Order execution always uses the in-memory latest price; no order-book/quote depth needed | Assignment §6.2/§7 scope ("use the latest available market price") | None within scope | N/A — assignment-specified | High |

## B. CONFIRMED facts (no longer assumptions — evidence in `docs/api-investigation.md`)

- ✅ Auth endpoint live and reachable; returns `401` + JSON envelope `{"success":false,"code":236,"message":"access_denied"}` for all bad/placeholder requests.
- ✅ Error envelope shape `{ success: bool, code: int, message: string }`.
- ✅ WS endpoint live; genuine WebSocket upgrade endpoint; validates `token` **query parameter** at handshake; invalid token → `HTTP/1.1 401`, body `null`.
- ✅ Integration sequence: POST auth → token → `ws://…/ws?token={TOKEN}` (per assignment §3, handshake mechanism confirmed by probe).

## C. UNKNOWN / BLOCKED (requires valid provider credentials)

- ❌ U1 Auth request body schema → blocks AuthService implementation (Phase 2)
- ❌ U2 Token success-response shape → blocks token parsing (Phase 2)
- ❌ U3 Token lifetime → blocks refresh policy tuning (Phase 2/3)
- ❌ U4 **Live price message schema** → blocks `PriceMessageParser` finalization (Phase 3/4)
- ❌ U5 Feed rate + symbol universe → blocks throttle tuning
- ❌ U6 Server disconnect/re-auth behavior → blocks reconnect-loop fine-tuning (Phase 3)

**Mitigation for all U-items (assignment-sanctioned):** build `PriceFeedService` and `AuthService` against interfaces with a **mock implementation** (`MockPriceFeedService` emitting realistic ticks) so Phases 1–8 can proceed and be demoed; swap in the live implementation the moment credentials unblock the probes. Every mock usage must be disclosed in the final delivery's assumptions/limitations list.

## D. Standing design assumptions (architectural, assignment-derived)

- D1: Browser never sees the provider token — backend mediates all provider contact. *(assignment §15: no tokens in source; also best practice)*
- D2: Single backend process holds the WS connection and latest-price memory store; SQL Server is only for trades.
- D3: "Server time preferred" for trade timestamps → store UTC (`DATETIME2`), display local.
- D4: `Status` limited to `Filled` / `Rejected` per assignment §8; validation failures may be represented as rejected orders or 4xx responses — we use 4xx for invalid requests and persist only executed (Filled) trades, which satisfies "simple status is acceptable".
- D5: `TradeId` is an INT IDENTITY primary key in storage; the assignment's `TRD10018`-style id is a display format applied in API DTOs (Phase 5), not in the database. *(Phase 1 decision)*
- D6: EF provider is hardwired to SQL Server via `UseSqlServer` — the local project dropped the workspace's `Database:Provider` switch class (`DatabaseOptions`), since local development has a real SQL Server 2022 instance and the Sqlite/InMemory fallback packages were deliberately not installed. Connection string key: `ConnectionStrings:DefaultConnection` (localhost, Windows auth, no secrets). Migrations applied via VS Package Manager Console (`Update-Database`). *(Phase 1 local decision, 2026-08-27)*
---
*Last updated: 2026-08-27 — Phase 1 added D5, D6.*
*Maintainers: update this file whenever a probe validates/invalidate an assumption. Mark validated items ✅ with the date and evidence link.*