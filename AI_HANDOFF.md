# AI_HANDOFF.md — Real-Time Mini Trading Platform (LOCAL PROJECT)

> **⚠️ WORKFLOW MODE (updated 2026-08-27): LOCAL-GUIDED DEVELOPMENT.**
> The user builds **locally in Visual Studio 2022**; the Arena workspace is a **planning/reference workspace only** (frozen reference scaffold + probe evidence — not authoritative). The AI gives small numbered steps; the user performs them locally and reports back; the AI verifies; then next step. The AI must explain every artifact per the learning rule (file name, exact location, why, responsibility, architecture layer, used-by; packages: name, why, phase, required/optional) and must never create/modify anything in the workspace on the user's behalf or advance phases without explicit go-ahead.

**Purpose:** continuity document — any AI agent can resume from this exact point without conversation history.
**Last updated:** 2026-08-29 · **Current phase:** Phase 8 COMPLETE ✅ (Serilog structured logging + temp-controller cleanup) · **Phase 9 (Unit tests) NOT STARTED**

---

## 1. Project mission

Build a **Real-Time Mini Trading Platform** (candidate assignment): an ASP.NET Core backend authenticates against a provider REST endpoint to get a token, connects to the provider WebSocket price feed, keeps latest price per symbol in memory, streams throttled updates to a React dashboard via SignalR, accepts Buy/Sell orders executed at the live price, stores trades in SQL Server, and updates history/positions without page reloads. Mobile-responsive UI required (1440 / 768 / 375 px).
**Time budget:** 6–10 hours (clean working prototype over over-engineering).

## 2. Project documents (local, solution-level)

| File | Purpose |
|---|---|
| HR assignment PDF (5 pages; a text-expanded variant also exists) | Source of truth — requirements, endpoints, evaluation weights |
| `docs/trading-platform-plan.md` | Master roadmap — do not change without explicit decision |
| `docs/assumptions.md` | Living assumptions register (updated 2026-08-28: auth closed) |
| `docs/api-investigation.md` | Provider probe evidence (+ §2.4 = the PROVEN auth spec) |
| `README.md` | Human/GitHub-facing doc — grows progressively, completed in Phase 10 |
| `AI_HANDOFF.md` | This file — update after every completed phase/step batch |

## 3. LOCKED technology stack (do not substitute)

ASP.NET Core 8 (`net8.0`, do NOT retarget) · C# · SignalR (built into the framework; npm client later) · EF Core 8 (8.0.30) · SQL Server · React + Vite + **JavaScript (NOT TypeScript)** · xUnit.

## 4. Rules of engagement (binding)

1. No .NET version or NuGet version changes without explicit user instruction.
2. No TypeScript anywhere; frontend stays JavaScript.
3. No unnecessary libraries; nothing installed without stating why/phase/required-optional.
4. Step-by-step only: instruct → user performs → verify → next. Never auto-advance a phase; STOP after each.
5. Learning rule always applies (see workflow header).
6. No credentials/tokens in source or appsettings.json — user-secrets only (enabled). **Never paste real credentials/tokens into chat; mask log lines that contain raw response bodies.**
7. Do not invent provider schemas not yet observed; new wire formats must be captured from live evidence first.

## 5. Local environment facts (updated 2026-08-28)

| Item | State |
|---|---|
| IDE / Runtime | Visual Studio 2022; .NET 8 only (10 installed on machine — must not be used) |
| SQL Server | Full SQL Server 2022 (16.0.1000.6), `Server=localhost`, Windows auth |
| App URLs (dev) | `https://localhost:7206` (Swagger), `http://localhost:5113` |
| Connection key | `ConnectionStrings:DefaultConnection` |
| Packages (Api) | EF Core SqlServer/Design/Tools 8.0.30, Swashbuckle 6.6.2 (template). Nothing else — no new packages needed through Phase 3 |
| Migration workflow | VS Package Manager Console; Default project = TradingPlatform.Api |
| Git | Local repo `main`; commit checkpoint at each phase boundary (via VS Git Changes or GitHub Desktop) |
| **Provider credentials** | ✅ **RECEIVED from HR (2026-08-28)**: User ID (`csfx…`), Account ID (numeric), Password — stored in `secrets.json` only. **Digest identity = User ID**; Account ID unused for issuance (kept for possible later use) |
| **Provider auth (PROVEN)** | HTTP Digest, **MD5-only** server (crashes HTTP 500 on other algorithm tokens — .NET handler unusable), **User-Agent required** at login, success envelope `{"success":true,"message":"token","result":"<8-char token>"}`, no expiry. Full spec: docs/api-investigation.md §2.4 |
| Test project | Not created yet (Phase 9) |

## 6. Local project state (2026-08-28, post Phase 2.5; Phase 3 Step 12 issued)
C:\Users\patha\source\repos\TradingPlatform
├─ TradingPlatform.sln
├─ AI_HANDOFF.md, README.md
├─ docs\ (trading-platform-plan.md, assumptions.md, api-investigation.md)
├─ db\schema.sql
└─ TradingPlatform.Api
├─ Program.cs ← AddDbContext; Configure<AuthApiOptions>; AddHttpClient<IAuthService,AuthService>
│ (BaseAddress+15s+default User-Agent; NO handler credentials); temp startup smoke-log
├─ appsettings.json ← ConnectionStrings:DefaultConnection; AuthApi (BaseUrl/TokenPath); [Feed section = Step 12, pending]
├─ Domain\Trade.cs, TradeSide.cs, TradeStatus.cs
├─ Exceptions\AuthException.cs
├─ Infrastructure\Persistence\TradingDbContext.cs (+ Configurations\TradeConfiguration.cs)
├─ Infrastructure\Services\IAuthService.cs, AuthService.cs
│ ← Phase 2.5: MANUAL MD5 Digest handshake (challenge → answer),
│ TryAddWithoutValidation raw header, {} bodies, tolerant parser
│ incl. "result" token field; token cache + InvalidateToken()
├─ Migrations\20260827150445_InitialCreate.cs (+ Designer, Snapshot)
├─ Options\AuthApiOptions.cs ← Username/AccountId/Password/BaseUrl/TokenPath
├─ Controllers\WeatherForecastController.cs, WeatherForecast.cs ← placeholders (Phase 5 removal)
├─ Controllers\AuthProbeController.cs ← TEMPORARY auth verification (Phase 5 removal)
└─ Properties\launchSettings.json


**Phase 3 Step 12 (issued, pending user confirmation):** `Options/FeedOptions.cs`, `Models/PriceTick.cs` (+ new `Models` folder), `Infrastructure/Services/IPriceStore.cs`, `Infrastructure/Services/InMemoryPriceStore.cs`; appsettings `Feed` section; `Program.cs` — `Configure<FeedOptions>` + `AddSingleton<IPriceStore, InMemoryPriceStore>`.

## 7. Phase records

### 7. Phase 1 (COMPLETE locally, committed 2026-08-27)
Config + EF Core data layer. `dbo.Trades` (TradeId INT IDENTITY PK · Symbol NVARCHAR(16) · Side NVARCHAR(4) CHECK · Quantity DECIMAL(18,2) CHECK>0 · Price DECIMAL(18,5) · TimestampUtc DATETIME2(3) · Status NVARCHAR(10) CHECK · IX_Symbol · IX_TimestampUtc). Migration `20260827150445_InitialCreate` applied + verified in SSMS. Decisions D5/D6. Build 0 errors. `db/schema.sql` exported.

### 7b. Phase 2 (COMPLETE locally, committed 2026-08-27)
Auth foundation: `AuthApiOptions`, `IAuthService`/`AuthService`, `AuthException`, temp `AuthProbeController` (502 = upstream failure), user-secrets wired. Live-verified to the 401 round trip (placeholder creds). No token yet (schema unknown then).

### 7c. Phase 2.5 — AUTH SOLVED (2026-08-28, live-verified 200 OK)
**HR supplied real credentials** (User ID / Account ID / Password). Debugging campaign root-caused three server behaviors (full evidence: api-investigation §2.1/§2.4):
1. **MD5-only Digest**: any other `algorithm=` token → HTTP 500 Ada CONSTRAINT_ERROR. Challenge omits `algorithm=` → .NET handler defaults SHA-256 → handler-based attempts crashed pre-auth. Fix: **manual RFC 2617 MD5 handshake in AuthService** (`TryAddWithoutValidation` raw header; `{}` bodies).
2. **User-Agent required**: login inserts UA into `SESSN.USER_AGENT` (schema EFOREX138); missing → `ORA-01400` → 401. Fix: default UA on the auth HttpClient.
3. **Token field = `result`**: `{"success":true,"message":"token","result":"<8 chars>"}`; no expiry → cache until InvalidateToken(). Parser updated.
**Identity:** Digest username = **User ID** (`csfx…`); Account ID not used for issuance (kept in secrets).
**Live result:** probe → 200 `token acquired (length 8, expiresInSec=n/a)`. U1/U2/U3 CLOSED.
**Note:** a live token leaked into chat once (via a raw log paste) — replaced by app restart; never paste raw response-body logs.

### 7d. Phase 6 & 7 — React live dashboard + Order/History/Positions UI (COMPLETE, verified 2026-08-29)

Built entirely **locally** (Visual Studio + npm/Vite; the Arena workspace never contained these files — this project's frontend is JS-only, never the workspace's frozen TS scaffold). Delivered in small numbered steps (25–31), each proposed by the AI as full file text, applied by the user, then verified before advancing:

- **Step 25** — `trader-web/` scaffolded via `npm create vite@latest` (React + **JavaScript**, no TS). Base project structure only.
- **Step 26** — `signalr/connection.js` (`createMarketConnection()` factory, single shared hub connection to `/hubs/market`), `signalr/MarketDataContext.jsx` (provider exposing `status`/`prices`), `components/ConnectionBanner.jsx` (4-state pill: Connected/Connecting/Disconnected/Error).
- **Step 27** — `hooks/usePriceFlash.js`, `components/PriceRow.jsx`, `components/PriceCard.jsx`, `components/PriceTable.jsx` — desktop/tablet `<table>`, mobile stacked cards (≤576px breakpoint), green/red flash on price change, reuses the one shared SignalR connection (no second connection opened).
- **Step 28** — `api/config.js` (`API_BASE_URL`), `api/orders.js` (`placeOrder`), `toast/ToastContext.jsx` + `Toast.css` (app-wide toast), `components/OrderTicket.jsx` + `.css` — Quick Trade panel (symbol select, Buy/Sell, quantity, live price readout, client-side `MAX_QUANTITY=1000` UX mirror of the Step 20 backend cap), side-by-side with `PriceTable` on desktop, stacked on mobile.
- **Step 29** — `api/trades.js` (`fetchTrades`), `trades/TradeHistoryContext.jsx` (`TradeHistoryProvider`/`useTradeHistory`; loads latest 10 from `GET /api/trades` on mount; `addTrade()` prepends the POST-order response without a re-fetch, capped at 10), `components/TradeHistory.jsx` + `.css` (table/card, Buy=green/Sell=red). `OrderTicket` edited to call `addTrade(result)` on success.
- **Step 30** — `api/positions.js` (`fetchPositions`), `positions/PositionsContext.jsx` (`PositionsProvider`/`usePositions`; `refresh()` re-fetches `GET /api/positions` — backend `PositionCalculator` remains the sole PnL source, no client-side math duplication; also self-polls every 3s so `unrealizedPnL`/`currentPrice` drift live between trades), `components/PositionSummary.jsx` + `.css` (table/card; long=green/short=red qty; positive/negative PnL coloring; null avgPrice on flat positions rendered as `—`). `OrderTicket` edited to call `refreshPositions()` after `addTrade()`.
- **Step 31** — App Shell: `components/Sidebar.jsx` + `.css` (desktop persistent 220px sidebar with TradeDesk branding and 4 nav items — 📊 Dashboard [active], 📜 Trade History, 💼 Positions, ⚙️ Settings — presentational only, no React Router/routes added; tablet ≤900px collapses to a 64px icon-only rail; mobile ≤576px hidden by default, slides in via `translateX` as an overlay with dimmed backdrop + close button + Escape-key handler), `components/Header.jsx` + `.css` (sticky top bar: hamburger button visible only ≤576px, app title, `ConnectionBanner` moved inline here instead of its old floating fixed-position pill), `App.jsx`/`App.css` rewritten around a `.app-layout` flex wrapper (`Sidebar` + `.app-layout__main` containing `Header` + `main.app__content`), `ConnectionBanner.css` edited to drop the old floating/sticky rules (superseded by Header) and add a mobile-only rule collapsing to just the status dot.
  - **Bug caught during Step 31 and fixed:** the Escape-key `useEffect` added to `App.jsx` referenced `useEffect` without updating the top import line (`import { useState } from "react"`), throwing `ReferenceError: useEffect is not defined` on render → blank black page. Fixed by importing `{ useState, useEffect }`. Root-caused via terminal (`npm run dev` logs clean → ruled out build error) + reviewing the pasted `App.jsx` source directly. Confirmed working after the one-line fix.

**Result:** all 5 reference-UI panels (Connection status, Live Prices, Quick Trade, Trade History, Positions) are functionally complete, independently verified (including agent-side PnL arithmetic cross-checks against long/short/flat positions), and wrapped in a responsive App Shell. Verified by the user via screenshots at 1440/768/375-class widths (and DevTools responsive emulation) at each step — no known open UI bugs.

### 7e. Phase 8 — Hardening (COMPLETE, verified 2026-08-29)

Backend-only, `TradingPlatform.Api` (this repo checkout, not `trader-web/`). Two independent pieces, delivered as one combined change set per the same inspect → propose → user-applies → verify workflow:

1. **Serilog structured logging.** Added `Serilog.AspNetCore` **8.0.3** (matches locked `net8.0` target — the newer `10.0.0` targets net9/net10 only, so it was rejected). Console sink only (user's choice; no file sink). `Program.cs` restructured around Serilog's recommended two-stage bootstrap pattern: a `CreateBootstrapLogger()` catches host-startup failures, then `builder.Host.UseSerilog(...)` builds the final logger from `appsettings.json`'s new `"Serilog"` section (`MinimumLevel:Default`/`Override` — replaces the old `"Logging"` section, renamed in both `appsettings.json` and `appsettings.Development.json`). `app.UseSerilogRequestLogging()` added right after the existing `ExceptionHandlingMiddleware` registration, condensing ASP.NET's normally-noisy per-request logging into one clean line per request (`HTTP GET /api/positions responded 200 in 9.3ms`). The whole `app.Run()` call now sits inside a `try/catch/finally` with `Log.Fatal`/`Log.CloseAndFlush()`, which also let the old TEMPORARY `Console.WriteLine($"[startup] AuthApi bound: ..."` auth-config diagnostic block be removed (redundant now that Serilog logs startup state properly).
2. **Temporary/scaffold code removal.** Deleted `Controllers/AuthProbeController.cs` (explicitly marked "remove in Phase 5" in its own doc comment — the Phase 2/2.5/3 diagnostic endpoints `/api/authprobe`, `/discover`, `/instruments`, `/feedconfig`), `Controllers/WeatherForecastController.cs`, and `WeatherForecast.cs` (default ASP.NET template scaffold, never used). Also removed several blocks of dead commented-out `AddHttpClient<IAuthService, AuthService>` code in `Program.cs` left over from early Phase 2/2.5 auth debugging — the currently-used named-`HttpClient`+manual-Digest approach (`AddHttpClient("AuthApi", ...)` + `AddSingleton<IAuthService>(...)`) was preserved byte-for-byte.

**Verified by user:** clean Serilog console output on startup (including the pre-existing `MOCK FEED ACTIVE` warning now rendering as a proper leveled `WRN` line), Swagger UI confirmed to list only Health/Prices/Orders/Trades/Positions (no AuthProbe/WeatherForecast), and a full end-to-end dashboard flow re-tested (place order → toast → Trade History prepend → Positions refresh/poll) with no regressions. No DI registrations, hosted services, CORS policy, or middleware ordering changed from the already-verified Phase 5 setup — only logging infrastructure and dead-code removal.

## 8. Phase roadmap status

| Phase | Scope | Status |
|---|---|---|
| 0 | Probe APIs + docs | ✅ COMPLETE |
| 1 | Config + EF Core data layer | ✅ COMPLETE (SSMS-verified, committed) |
| 2 | AuthService foundation | ✅ COMPLETE (committed) |
| 2.5 | Auth resolution (Digest reverse-engineering) | ✅ **COMPLETE — token acquisition live-verified** |
| 3 | Price feed (live WS + mock fallback) | ✅ **COMPLETE** — `FeedOptions`/`PriceTick`/`IPriceStore`/`InMemoryPriceStore`/`FeedStateService`/`PriceMessageParser`/`LivePriceFeedService`/`MockPriceFeedService` all implemented; dual-mode via `Feed:Mode` (committed `e1fba90`) |
| 4 | SignalR throttled broadcast | ✅ **COMPLETE (verified 2026-08-28)** — `MarketHub` (`/hubs/market`, `"market"` group, snapshot-on-connect) + `MarketBroadcastService` (300 ms throttled `"prices"` batches); manual browser-console SignalR client confirmed snapshot + ~3/sec batched updates |
| 5 | REST endpoints + orders | ✅ **COMPLETE (verified 2026-08-28)** — `GET /api/prices`, `GET /api/health`, `POST /api/orders`, `GET /api/trades`, `GET /api/positions`, `TRD100xx` formatting, global `ExceptionHandlingMiddleware`, `OrderRequestValidator` (FluentValidation) — all step-by-step verified via Swagger + SQL Server checks. *(Temporary `AuthProbeController`/`WeatherForecastController` not yet removed — deferred, not required for functionality.)* |
| 6 | React live dashboard | ✅ **COMPLETE (verified 2026-08-29)** — Vite+React+JS scaffold; shared SignalR connection (`MarketDataContext`) driving `PriceTable` with price-flash; responsive at 1440/768/375 throughout |
| 7 | Order ticket, history, positions UI | ✅ **COMPLETE (verified 2026-08-29)** — `OrderTicket` (Quick Trade, toast feedback), `TradeHistory` (live prepend via `TradeHistoryContext`, latest-10 cap), `PositionSummary` (backend-sourced PnL via `GET /api/positions`, post-trade refresh + 3s poll); App Shell (`Sidebar` + `Header`) wraps all panels with responsive nav (desktop persistent sidebar, tablet icon rail, mobile hamburger slide-out overlay) |
| 8 | Hardening (Serilog; remove temp logs/probe) | ✅ **COMPLETE (verified 2026-08-29)** — `Serilog.AspNetCore` 8.0.3 added (console sink, `ReadFrom.Configuration`, two-stage bootstrap init, `UseSerilogRequestLogging()`); `AuthProbeController.cs`, `WeatherForecastController.cs`, `WeatherForecast.cs` deleted; temp startup `Console.WriteLine` auth-config dump and dead commented-out `AddHttpClient<IAuthService, AuthService>` blocks removed from `Program.cs`; `appsettings.json`/`appsettings.Development.json` `"Logging"` section renamed to `"Serilog"` (`MinimumLevel.Default`/`Override`) |
| 9 | Unit tests (xUnit project) | ⬜ **NOT STARTED** — next up |
| 10 | Docs & delivery (README final, .gitignore review, screenshots/demo) | ⬜ |

## 9. Decision log / exact next steps

**2026-08-27:** Phase 3 held (no credentials). **2026-08-28:** credentials received → auth investigation ran (T0–T2 body shapes → OPTIONS probe revealed Digest → MD5 bisection → UA discovery → `result` field) → **auth solved**. User approved live-first Phase 3 with mock fallback behind `Feed:Mode`. Phase 3 completed and committed (`e1fba90`).

**2026-08-28 (continued):** Phases 4 and 5 built and verified step-by-step (Steps 16–24), then committed and pushed to `main` (`b7e842e "Complete Phase 4 and Phase 5"`):
- Step 16: `MarketHub` + SignalR registration + CORS + snapshot-on-connect.
- Step 17: `MarketBroadcastService` — 300 ms throttled diff-based `"prices"` broadcast to the `"market"` group.
- Step 18: `GET /api/prices` (`PricesController`).
- Step 19: `GET /api/health` (`HealthController`).
- Step 20: `POST /api/orders` (`OrdersController`, `Contracts/OrderRequest`/`OrderResponse`) — manual validation initially, `TRD100xx` formatting.
- Step 21: `GET /api/trades` (`TradesController`, `Contracts/TradeDto`) — paged, newest-first.
- Step 22: `GET /api/positions` (`PositionsController`, `IPositionCalculator`/`PositionCalculator`, `Contracts/PositionDto`) — average-cost netting, long/short support, realized/unrealized PnL, null-safe when no live price.
- Step 23: `Middleware/ExceptionHandlingMiddleware` — global `{ error, traceId }` JSON envelope, logged server-side, registered first in the pipeline.
- Step 24: `Validators/OrderRequestValidator` (FluentValidation package added) — replaced manual checks in `OrdersController`, invoked manually (not auto-wired into `ModelState`) to preserve the existing `{ error }` response shape. *(Hit and resolved a DI-registration placement bug: the `AddScoped<IValidator<OrderRequest>, OrderRequestValidator>()` call must be registered before `builder.Build()`, not after.)*

**2026-08-29:** Phase 6 (React live dashboard) and Phase 7 (Order ticket/history/positions UI) built and verified step-by-step (Steps 25–31, see §7d) entirely in the user's local `trader-web/` project (not present in this repo checkout). All 5 reference-UI panels plus the responsive App Shell (sidebar + header) are complete and confirmed working at 1440/768/375-class widths. User elected to proceed to **Phase 8 (Hardening)** next.

**2026-08-29 (continued):** Phase 8 (Hardening) completed and verified — see §7e. Serilog structured logging added (`Serilog.AspNetCore` 8.0.3, console sink), temporary `AuthProbeController`/`WeatherForecastController`/`WeatherForecast.cs` removed, dead commented-out code cleaned from `Program.cs`. User confirmed clean startup logs, Swagger UI free of removed endpoints, and full end-to-end dashboard flow still working post-change.

**Immediate next actions:**
1. Start **Phase 9 — Unit tests**: set up an xUnit test project (e.g. `TradingPlatform.Api.Tests`) covering key backend logic — prime candidates: `IPositionCalculator`/`PositionCalculator` (netting + PnL math, already informally verified via manual PnL cross-checks during Step 30 but not covered by automated tests), `OrderRequestValidator` (FluentValidation rules from Step 24), and possibly the `TRD100xx` trade-ID formatting logic in `OrdersController`.
2. Continue the same step-by-step, user-applies/agent-never-edits-locally workflow used for Phases 6–8.

---
*End of handoff. Update at the end of every completed phase (and major step batches). Do not skip §4 rules.*