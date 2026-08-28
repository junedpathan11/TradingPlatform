# Real-Time Trading Platform — Project Plan (.NET Core)

**Assignment:** Real-Time Mini Trading Platform — Candidate Evaluation Task
**Stack:** ASP.NET Core 8 Web API + SignalR · React (Vite) SPA · SQL Server + EF Core
**Effort budget:** 6–10 hours (this plan is time-boxed to ~8.5–9 hours, with a ~6-hour MVP cut-off path)
**Status:** Plan only — no code written yet

---

## 1. What we're building (one paragraph)

A mini trading platform: the **ASP.NET Core backend** authenticates against the provided REST endpoint to get a token, opens the provider WebSocket with that token, parses streaming prices, keeps the **latest price per symbol in memory**, and pushes throttled updates to a **React dashboard via SignalR**. The user picks a symbol, hits Buy/Sell with a quantity; the backend validates against the latest price, **stores the trade in SQL Server**, and the frontend updates trade history + position/PnL **without any page reload**.

### Evaluation weights → build priorities

| Weight | Area | Implication for us |
|---|---|---|
| **25%** | API auth & WebSocket integration | The heart of the assignment. Reconnect logic and token flow must be flawless and visible. |
| **20%** | Backend design & code quality | Clean layered services, validation, structured logging. |
| **20%** | Frontend UX & live updates | No page reloads, connection status, smooth throttled updates, responsive. |
| **15%** | Trade handling & storage | Correct order → trade record with latest price + timestamp. |
| **10%** | Performance approach | Throttling/batching, latest-price cache, no UI freezing. |
| **10%** | Documentation | README, assumptions, architecture notes, screenshots/demo. |

> Rule of thumb: if we run out of time, we cut **unit tests and bonus polish** first — never auth flow, reconnect logic, or trade storage.

---

## 2. Technology stack (decided)

| Layer | Choice | Why |
|---|---|---|
| Runtime | **.NET 8 (LTS)** | Safe default reviewers can run; swap to .NET 10 LTS if that's what your machine has — plan is unchanged. |
| API | ASP.NET Core **Web API** (controllers or minimal APIs) | Required/conventional. |
| Real-time to browser | **SignalR** | Assignment explicitly prefers SignalR for .NET solutions. |
| Upstream feed | `ClientWebSocket` in a **BackgroundService** | Full control over connect/auth/reconnect lifecycle. |
| Frontend | **React 18 + Vite + TypeScript**, `@microsoft/signalr` client | Chosen by team; fast dev loop, clean dashboard. |
| ORM | **EF Core 8** (code-first + migrations) | Assignment requires schema/migration deliverable. |
| Database | **SQL Server** (LocalDB or Express, or `mcr.microsoft.com/mssql/server` via Docker) | Chosen by team; easy for reviewers via connection-string env var. |
| Validation | FluentValidation (or built-in DataAnnotations) | Clean, testable order validation. |
| Logging | `ILogger` everywhere; **Serilog** console sink as easy structured-logging bonus | Bonus points item. |
| API docs | **Swashbuckle (Swagger UI)** | Bonus points item. |
| Tests | **xUnit** | Bonus points item. |

**Endpoints to integrate (from assignment):**

- REST auth: `POST http://s138.acttrader.com:10138/api/v2/auth/token`
- WebSocket: `ws://s138.acttrader.com:22138/ws?token={TOKEN}`

---

## 3. Architecture

```
┌─────────────────────────┐         ┌──────────────────────────────────────────────┐
│  React SPA (Vite)       │  REST   │              ASP.NET Core 8 Host             │
│                         │────────▶│                                              │
│  • Connection status    │ SignalR │  Controllers:                                │
│  • Live price table     │◀────────│   /api/health /api/prices                    │
│    (green/red flash)    │  (push) │   /api/orders /api/trades /api/positions     │
│  • Order ticket         │         │                                              │
│  • Trade history        │         │  Services:                                   │
│  • Positions & PnL      │         │   IAuthService ──▶ POST /api/v2/auth/token   │
│                         │         │        │ token                               │
│  Throttled rendering    │         │        ▼                                     │
│  (rAF / 250–500 ms)     │         │  PriceFeedService (BackgroundService)        │
└─────────────────────────┘         │   ClientWebSocket ──▶ ws://…/ws?token=…      │
                                    │   parse ▶ IPriceStore (latest per symbol)    │
                                    │   batch-flush ▶ SignalR hub broadcast        │
                                    │                                              │
                                    │  OrderService (validate vs PriceStore)       │
                                    │  TradeRepository ──▶ EF Core ──▶ SQL Server  │
                                    └──────────────────────────────────────────────┘
```

**Key decisions:**

1. **Token lives only in the backend.** The browser never sees the provider token (assignment: no credentials/tokens in source; safer design too).
2. **One writer for prices.** `PriceFeedService` (an `IHostedService`) is the single component that talks to the provider WebSocket. Everyone else reads from `IPriceStore`.
3. **In-memory latest-price cache** (`ConcurrentDictionary<string, PriceTick>`) for O(1) order execution — satisfies "keep latest price in memory" requirement.
4. **Throttle at the broadcast boundary**, not per tick: the feed service accumulates ticks and flushes a batch to SignalR every ~300 ms. The React side additionally throttles renders.
5. **Reconnect with exponential backoff + jitter** on the upstream WebSocket, and SignalR's built-in `withAutomaticReconnect` on the client. Status surfaces to UI.

---

## 4. Proposed solution structure

```
TradingPlatform.sln
├─ src/
│  ├─ TradingPlatform.Api/            # ASP.NET Core host
│  │  ├─ Program.cs                   # DI wiring, SignalR map, Swagger, CORS
│  │  ├─ Controllers/
│  │  │  ├─ PricesController.cs       # GET /api/prices
│  │  │  ├─ OrdersController.cs       # POST /api/orders
│  │  │  ├─ TradesController.cs       # GET /api/trades
│  │  │  ├─ PositionsController.cs    # GET /api/positions (optional)
│  │  │  └─ HealthController.cs       # GET /api/health
│  │  ├─ Hubs/MarketHub.cs            # SignalR hub (JoinMarket group)
│  │  ├─ Services/
│  │  │  ├─ IAuthService / AuthService.cs           # REST token flow + refresh
│  │  │  ├─ PriceFeedService.cs                     # BackgroundService, WS loop
│  │  │  ├─ IPriceStore / InMemoryPriceStore.cs     # latest tick per symbol
│  │  │  ├─ PriceMessageParser.cs                   # tolerant JSON parsing
│  │  │  ├─ IOrderService / OrderService.cs         # validate → build trade
│  │  │  └─ IPositionCalculator / PositionCalculator.cs
│  │  ├─ Repositories/
│  │  │  └─ TradeRepository.cs        # EF Core persistence
│  │  ├─ Models/ (DTOs, requests, responses)
│  │  ├─ Domain/ (Trade, Position entities)
│  │  ├─ Options/ (AppSettings bound classes: AuthApi, Feed, Throttle)
│  │  ├─ Exceptions/ (AuthException, OrderValidationException …)
│  │  └─ appsettings.json             # non-secret config only
│  ├─ TradingPlatform.Domain/         # (optional split; keeps Domain clean)
│  └─ trader-web/                     # React SPA (Vite + TS)
│     ├─ src/api/                     # REST client (fetch/axios)
│     ├─ src/signalr/                 # hub connection factory + auto-reconnect
│     ├─ src/hooks/ (usePrices, useConnectionStatus, useThrottledTick)
│     ├─ src/components/
│     │  ├─ ConnectionBanner.tsx
│     │  ├─ PriceTable.tsx            # flash green/red on change
│     │  ├─ OrderTicket.tsx           # symbol, side, qty → Buy/Sell
│     │  ├─ TradeHistory.tsx
│     │  ├─ PositionSummary.tsx
│     │  └─ layout/ (responsive shell: sidebar → mobile stack/menu)
│     └─ src/styles/                  # CSS grid, breakpoints 1440 / 768 / 375
├─ tests/
│  └─ TradingPlatform.UnitTests/      # parser, order validator, PnL, reconnect
├─ db/
│  └─ schema.sql                      # hand-runnable DDL for reviewers
├─ docs/
│  ├─ architecture.md
│  └─ screenshots/ (or demo video)
├─ docker-compose.yml                 # api + sql server (bonus)
└─ README.md
```

> Secrets: provider credentials go in `dotnet user-secrets` / environment variables — **never** in `appsettings.json` in source. The README explains the exact keys to set.

---

## 5. Phase-by-phase plan (time-boxed)

### Phase 0 — Probe the APIs & scaffold (≈ 40 min)

**Why first:** auth request format and WS message schema are *not fully specified* in the assignment. Probing early prevents a mid-build surprise and gives us real facts for the assumptions doc.

- [ ] `curl` / Postman the auth endpoint: try `POST` with JSON body (candidate formats: `{"username": "...", "password": "..."}` or `{"user": ..., "apiKey": ...}`); capture the exact token response shape (e.g. `{"token": "…"}` vs `{"accessToken": "…"}`, expiry field).
- [ ] Use the token manually against `ws://…/ws?token=…` (e.g. `wscat` or a 20-line C# console scratch) and capture **2–3 sample messages** → this defines the parser.
- [ ] `dotnet new sln`, add `TradingPlatform.Api`, scaffold React app (`npm create vite@latest trader-web -- --template react-ts`), add SignalR packages.
- [ ] Wire dev-time plumbing: CORS for `http://localhost:5173`, Swagger, `/api/health` returning `{ status, wsConnected }`.
- [ ] Record findings in `docs/assumptions.md` (started now, finished in Phase 10).

**Done when:** we know the real auth payload format, token response shape, and WS message schema — or have a documented fallback stub.

> **Fallback (documented assumption):** if the live endpoint is unreachable from our network, build a `MockPriceFeedService` behind the same `IPriceFeed` interface that emits realistic ticks, and note it clearly. The rest of the system is identical either way — this is why `PriceFeedService` is interface-driven from day one.

### Phase 1 — Config + EF Core data layer (≈ 40 min)

- [ ] Strongly-typed options classes bound from config: `AuthApiOptions` (BaseUrl, Username, Password), `FeedOptions` (WsUrl, ReconnectBaseDelayMs, MaxDelayMs), `BroadcastOptions` (FlushIntervalMs).
- [ ] Credentials via user-secrets: `dotnet user-secrets set "AuthApi:Username" "…"`.
- [ ] EF Core: `Trade` entity + `TradingDbContext`; SQL Server provider; connection string from env var with sane LocalDB default.
- [ ] Initial migration; also export plain `db/schema.sql` for reviewers who just want the DDL.
- [ ] Seed script not required, but include 2 sample trades behind a `--seed` flag for instant UI testing.

**Trade schema (assignment minimum fields):**

```sql
CREATE TABLE Trades (
    TradeId      INT IDENTITY(1,1) PRIMARY KEY,       -- displayed as TRD10001 style
    Symbol       NVARCHAR(16)  NOT NULL,              -- EURUSD
    Side         NVARCHAR(4)   NOT NULL,              -- Buy | Sell  (CHECK constraint)
    Quantity     DECIMAL(18,2) NOT NULL,              -- must be > 0
    Price        DECIMAL(18,5) NOT NULL,              -- latest price at execution
    TimestampUtc DATETIME2(3)  NOT NULL,              -- server time (UTC)
    Status       NVARCHAR(10)  NOT NULL               -- Filled | Rejected
);
CREATE INDEX IX_Trades_TimestampUtc ON Trades (TimestampUtc DESC);
CREATE INDEX IX_Trades_Symbol ON Trades (Symbol);
```

### Phase 2 — Authentication service (≈ 30 min)

- [ ] `AuthService.GetTokenAsync()`: typed `HttpClient` (factory) → `POST /api/v2/auth/token` with configured credentials → parse token (+ expiry if present).
- [ ] Cache the token in memory; re-auth on expiry or on WS auth-failure signal (used by Phase 3 reconnect logic).
- [ ] Structured logs: auth success, auth failure (with sanitized error — never log the password).
- [ ] Map failures to a clear `AuthException` → surfaces as `Error` state later.

**Done when:** a controller or scratch endpoint can fetch and log a live token on demand.

### Phase 3 — WebSocket price feed service (≈ 75 min) — the 25% core

- [ ] `PriceFeedService : BackgroundService`:
  1. get token from `IAuthService`
  2. `ClientWebSocket.ConnectAsync($"ws://…/ws?token={token}")`
  3. receive loop → `PriceMessageParser.Parse(raw)` → `IPriceStore.Update(tick)`
  4. duplicate/malformed/out-of-order messages handled in parser (skip + warn-log, never crash the loop)
- [ ] **Reconnect loop:** on disconnect/exception → exponential backoff (e.g. 1 s → 2 s → 4 s … capped 30 s, with jitter) → **re-auth first** (token may have expired) → reconnect → log every transition.
- [ ] Thread-safe stop/cancel handling; `CancellationToken` respected throughout.
- [ ] Expose `ConnectionState` (Disconnected / Connecting / Connected / Error) on a singleton `FeedStateService` for `/api/health` and the hub.

**Done when:** the service survives a forced server drop (kill test), reconnects, logs the whole lifecycle.

### Phase 4 — Price store + SignalR broadcast with throttling (≈ 45 min) ✅ COMPLETE (verified 2026-08-28)

- [x] `InMemoryPriceStore`: `ConcurrentDictionary<string, PriceTick>` keyed by symbol (Phase 3). *(Change%/Seq fields not added — deferred; changePct is instead computed at the broadcast/read boundary, see below.)*
- [x] Throttle: a dedicated `MarketBroadcastService` (`BackgroundService`) reads `IPriceStore` on a periodic flush (**300 ms**) and publishes only **latest tick per symbol** changed since the last flush → SignalR `"market"` group broadcast. (Decoupled from the feed services themselves, so it works identically regardless of Feed:Mode.)
- [x] `MarketHub` (SignalR) at `/hubs/market`: on connect, client joins the `"market"` group. *(Optional `SubscribePrice(symbol)` targeted-update method not implemented — not required for the verified group-broadcast flow.)*
- [x] On client connect, `MarketHub.OnConnectedAsync` immediately sends the current `IPriceStore` snapshot to the caller (late-joining browser sees prices instantly).
- [x] Payload contract implemented as documented:

```json
// SignalR event "prices" → array batch
{ "prices": [ { "symbol": "EURUSD", "price": 1.08348, "changePct": 0.12, "ts": "2026-08-27T06:15:30Z" } ] }
```

**Done when:** a console/browser test client sees batched updates at ~3/sec instead of per-tick spam. — ✅ **Verified**: manual `@microsoft/signalr` browser-console client received the initial 6-symbol snapshot plus continuous ~300 ms throttled batches, no CORS/connection errors (Mock feed mode).

### Phase 5 — REST endpoints + order handling (≈ 45 min) ✅ COMPLETE (verified 2026-08-28)

| Method | Route | Behavior | Status |
|---|---|---|---|
| GET | `/api/prices` | Latest tick per symbol from `IPriceStore` (snapshot) | ✅ Verified |
| POST | `/api/orders` | Validate → execute at latest price → persist → return confirmation | ✅ Verified |
| GET | `/api/trades` | Recent trades (paged, newest first) | ✅ Verified |
| GET | `/api/positions` | Net position + realized/unrealized PnL per symbol *(optional but targeted)* | ✅ Verified (long/short netting, average-cost accounting, partial closes, flat positions) |
| GET | `/api/health` | `{ api: ok, feed: Connected, lastTickAt, symbols, uptime }` | ✅ Verified |

**Order flow (backend):** request `{ symbol, side: Buy|Sell, quantity }` → validate (symbol exists in store, quantity > 0 and ≤ cap) → take price from `IPriceStore` (error 409-style response if no live price for symbol) → build `Trade` (status `Filled`; `Rejected` with reason for validation failures — assignment allows either) → persist via repository → return `{ tradeId: "TRD10001", ... , executedPrice, timestampUtc }`. *(Only `Filled` trades are ever persisted — validation failures return 4xx without writing a row; `Rejected` status is defined but not currently reachable, per docs/assumptions.md D4.)*

- [x] TradeId formatted `TRD100xx` on the response/DTO (identity PK stays int internally). Verified on `POST /api/orders` and `GET /api/trades`.
- [x] Global exception middleware → consistent `{ error }` JSON + logged; no stack traces to client. Verified via forced unhandled-exception test (500 response with `{ error, traceId }`, full exception logged server-side).
- [x] FluentValidation rules for the order request. `OrderRequestValidator` invoked manually in `OrdersController` (not wired into ASP.NET's automatic `ModelState` pipeline), preserving the existing `{ error: "..." }` response shape.

**Done when:** Swagger can round-trip: place order → see it in `/api/trades` with the live price at execution time. — ✅ **Verified**: EURUSD Buy 10 → `TRD10001` → confirmed row in `dbo.Trades` and in `GET /api/trades`; validation edge cases (qty ≤ 0, qty > cap, invalid side, unknown symbol) all return the correct 4xx/409 without persisting a row.

### Phase 6 — React live dashboard (≈ 75 min)

- [ ] App shell: responsive layout (desktop: sidebar + 2-column grid; tablet: stacked; mobile: single column + collapsible menu). Breakpoints **1440 / 768 / 375**.
- [ ] `signalr` connection factory with `withAutomaticReconnect([0, 2000, 5000, 10000])` + manual retry loop after that; connection state pushed into app state.
- [ ] `ConnectionBanner`: **Connected / Connecting / Disconnected / Error** — always visible (top bar pill on desktop, sticky header chip on mobile).
- [ ] `PriceTable`:
  - data from SignalR `prices` events; initial load from `GET /api/prices`
  - **green/red flash** on up/down tick (bonus item, cheap to add now)
  - renders throttled (update state at most ~4×/sec via a rAF or interval hook) — never per tick
  - mobile: compact card list or horizontal scroll (per assignment responsive spec)
- [ ] Loading and empty states; skeleton while first snapshot arrives.
- [ ] No page refresh anywhere — navigation within the SPA only.

**Done when:** prices tick live in the browser at 1440/768/375 with no jank and visible status.

### Phase 7 — Order ticket, history, positions (≈ 60 min)

- [ ] `OrderTicket`: symbol picker (from live symbols), side toggle **Buy/Sell**, qty input, live "current price" readout from the store, submit → POST `/api/orders`.
- [ ] On success: toast/inline confirmation with trade id + executed price; on failure: clear error message.
- [ ] `TradeHistory`: loads `GET /api/trades`, then prepends new trade from the POST response (and/or refreshes via SignalR `tradeExecuted` event) — **no reload**.
- [ ] `PositionSummary` (optional-but-targeted): net qty per symbol, avg price, unrealized PnL recomputed client-side from live prices; refreshed after each trade.
- [ ] Mobile usability pass: big-enough tap targets, no overlapping fields (assignment explicitly checks this).

**Done when:** full user journey — watch prices → place order → see history + PnL update instantly — works on desktop and phone widths.

### Phase 8 — Hardening & error handling (≈ 30 min)

- [ ] Matrix pass: auth failure (bad creds) → backend `Error` state + UI banner; WS drop → `Connecting` + auto-reconnect; invalid order (0/negative qty, unknown symbol) → 400 with reason; backend exception → 500 envelope, logged.
- [ ] Duplicate/out-of-order/delayed tick handling verified (defensive parser tests).
- [ ] Request logging middleware with correlation id; all important events logged (connect, auth, reconnect, order, errors).
- [ ] Serilog console sink (structured) — quick bonus win.

### Phase 9 — Unit tests (≈ 30 min, bonus)

- [ ] `PriceMessageParser` — valid, malformed, duplicate, out-of-order.
- [ ] `OrderService` — happy path, unknown symbol, bad quantity, no-price-yet case.
- [ ] `PositionCalculator` — netting, PnL math (Buy then Sell, partial).
- [ ] Reconnect/backoff delay sequence test (pure function).

### Phase 10 — Documentation & delivery (≈ 50 min) — the 10% that's easy to nail

- [ ] **README.md**: prerequisites, `dotnet user-secrets` setup, `dotnet ef database update`, run API + SPA, run via Docker (if included), troubleshooting.
- [ ] **docs/architecture.md**: the diagram above + data flow + throttling rationale (maps to "Performance Approach" criterion).
- [ ] **docs/assumptions.md**: every API assumption from Phase 0 (auth body format, token field, WS schema, ws:// non-TLS note, endpoint reachability).
- [ ] **db/schema.sql** + migration files committed.
- [ ] Screenshots (desktop + mobile 375px) or a 2–3 min demo video (auth → stream → order → history → reconnect).
- [ ] Known limitations list (e.g., single-user prototype, no auth for *our own* users, decimal rounding policy).
- [ ] Final responsive pass at 1440/768/375; run the full journey once from a clean checkout.

**Total: ≈ 8 h 40 m** (leaves buffer inside the 10 h cap).

---

## 6. MVP cut-off path (~6 hours)

If time is tight, this is the minimum that still scores well on every criterion:

1. Phases 0–5 **complete** (auth, feed, store, throttled SignalR, all required REST endpoints, SQL storage).
2. Phase 6 with a **simpler UI**: connection banner + price table + order ticket (skip flash animation, skeletons).
3. Phase 7 with trade history only (skip positions view; keep `/api/positions` endpoint if it fell out of the repository naturally).
4. Phase 10 README + assumptions only (skip video; take 2 screenshots).
5. **Skip** Phase 9 tests and Docker — note them as "known gaps" honestly.

Never skip: reconnect logic, `/api/health`, responsive check, or the assumptions doc — they're explicitly weighted or explicitly required.

---

## 7. Bonus scorecard (grab the cheap ones)

| Bonus | Effort | In plan? |
|---|---|---|
| SignalR frontend updates | 0 (core design) | ✅ Phases 4/6 |
| Green/red price flash | ~20 min | ✅ Phase 6 |
| Position summary + unrealized PnL | ~30 min | ✅ Phase 7 |
| Swagger/OpenAPI | ~10 min | ✅ Phase 0 |
| Structured logging (Serilog) | ~15 min | ✅ Phase 8 |
| Unit tests | ~30 min | ✅ Phase 9 (first to cut) |
| Clean architecture (services/repo separation) | 0 (by design) | ✅ throughout |
| Docker setup | ~30 min | ⬜ Only if ahead of schedule |
| Login screen | ~40 min | ⬜ Only if ahead of schedule |

---

## 8. Risks & assumptions (start of the assumptions doc)

| # | Risk / unknown | Mitigation |
|---|---|---|
| 1 | Auth request body format unspecified | Phase 0 probe; try documented candidates; **document the assumption** (assignment explicitly invites this). |
| 2 | WS message schema unspecified | Phase 0 capture; write a tolerant parser (case-insensitive keys, ignore unknowns). |
| 3 | Provider endpoints are plain `http://` / `ws://` on unusual ports — may be blocked by corporate networks/firewalls | Test early; if blocked, document + use the `MockPriceFeedService` stub behind the same interface. |
| 4 | Token lifetime unknown | Re-auth on WS auth-failure + proactive refresh if expiry present in response. |
| 5 | Feed could be very fast | Throttle at broadcast (300 ms) + UI-level render throttle; latest-price dictionary is O(1). |
| 6 | SQL Server availability for the reviewer | `docker-compose.yml` with SQL Server + env-var connection string + `schema.sql` fallback. |
| 7 | Time overrun | MVP cut-off path (§6); cut tests/Docker first, never auth/WS/storage. |

---

## 9. Definition of done (pre-submission checklist)

- [ ] Auth → token → WS connect works end-to-end with credentials from user-secrets (nothing hardcoded)
- [ ] Prices stream to browser via SignalR; UI updates with **zero page reloads**
- [ ] Connection status pill reflects real state; survives a forced disconnect + reconnect
- [ ] Buy/Sell order executes at the **live** price, persists to SQL Server, returns `TRD…` confirmation
- [ ] Trade history + positions update immediately after the order
- [ ] Order validation rejects bad input with a clear message; backend never 500s on bad input
- [ ] Malformed/duplicate ticks don't crash anything (verified in logs)
- [ ] Looks correct at 1440 / 768 / 375 px — tables readable, order panel usable, nothing clipped
- [ ] `GET /api/health` shows API + feed status; Swagger browsable
- [ ] README runs the project from a clean clone; `db/schema.sql` present; assumptions doc complete
- [ ] Screenshots (3 widths) / short demo recorded
- [ ] Source zipped/repo link ready