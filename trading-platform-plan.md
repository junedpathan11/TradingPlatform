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
┌─────────────────────────┐ ┌──────────────────────────────────────────────┐
│ React SPA (Vite) │ REST │ ASP.NET Core 8 Host │
│ │────────▶│ │
│ • Connection status │ SignalR │ Controllers: │
│ • Live price table │◀────────│ /api/health /api/prices │
│ (green/red flash) │ (push) │ /api/orders /api/trades /api/positions │
│ • Order ticket │ │ │
│ • Trade history │ │ Services: │
│ • Positions & PnL │ │ IAuthService ──▶ POST /api/v2/auth/token │
│ │ │ │ token │
│ Throttled rendering │ │ ▼ │
│ (rAF / 250–500 ms) │ │ PriceFeedService (BackgroundService) │
└─────────────────────────┘ │ ClientWebSocket ──▶ ws://…/ws?token=… │
│ parse ▶ IPriceStore (latest per symbol) │
│ batch-flush ▶ SignalR hub broadcast │
│ │
│ OrderService (validate vs PriceStore) │
│ TradeRepository ──▶ EF Core ──▶ SQL Server │
└──────────────────────────────────────────────┘


**Key decisions:**

1. **Token lives only in the backend.** The browser never sees the provider token (assignment: no credentials/tokens in source; safer design too).
2. **One writer for prices.** `PriceFeedService` (an `IHostedService`) is the single component that talks to the provider WebSocket. Everyone else reads from `IPriceStore`.
3. **In-memory latest-price cache** (`ConcurrentDictionary<string, PriceTick>`) for O(1) order execution — satisfies "keep latest price in memory" requirement.
4. **Throttle at the broadcast boundary**, not per tick: the feed service accumulates ticks and flushes a batch to SignalR every ~300 ms. The React side additionally throttles renders.
5. **Reconnect with exponential backoff + jitter** on the upstream WebSocket, and SignalR's built-in `withAutomaticReconnect` on the client. Status surfaces to UI.

---

## 4. Proposed solution structure
TradingPlatform.sln
├─ src/
│ ├─ TradingPlatform.Api/ # ASP.NET Core host
│ │ ├─ Program.cs # DI wiring, SignalR map, Swagger, CORS
│ │ ├─ Controllers/
│ │ │ ├─ PricesController.cs # GET /api/prices
│ │ │ ├─ OrdersController.cs # POST /api/orders
│ │ │ ├─ TradesController.cs # GET /api/trades
│ │ │ ├─ PositionsController.cs # GET /api/positions (optional)
│ │ │ └─ HealthController.cs # GET /api/health
│ │ ├─ Hubs/MarketHub.cs # SignalR hub (JoinMarket group)
│ │ ├─ Services/
│ │ │ ├─ IAuthService / AuthService.cs # REST token flow + refresh
│ │ │ ├─ PriceFeedService.cs # BackgroundService, WS loop
│ │ │ ├─ IPriceStore / InMemoryPriceStore.cs # latest tick per symbol
│ │ │ ├─ PriceMessageParser.cs # tolerant JSON parsing
│ │ │ ├─ IOrderService / OrderService.cs # validate → build trade
│ │ │ └─ IPositionCalculator / PositionCalculator.cs
│ │ ├─ Repositories/
│ │ │ └─ TradeRepository.cs # EF Core persistence
│ │ ├─ Models/ (DTOs, requests, responses)
│ │ ├─ Domain/ (Trade, Position entities)
│ │ ├─ Options/ (AppSettings bound classes: AuthApi, Feed, Throttle)
│ │ ├─ Exceptions/ (AuthException, OrderValidationException …)
│ │ └─ appsettings.json # non-secret config only
│ ├─ TradingPlatform.Domain/ # (optional split; keeps Domain clean)
│ └─ trader-web/ # React SPA (Vite + TS)
│ ├─ src/api/ # REST client (fetch/axios)
│ ├─ src/signalr/ # hub connection factory + auto-reconnect
│ ├─ src/hooks/ (usePrices, useConnectionStatus, useThrottledTick)
│ ├─ src/components/
│ │ ├─ ConnectionBanner.tsx
│ │ ├─ PriceTable.tsx # flash green/red on change
│ │ ├─ OrderTicket.tsx # symbol, side, qty → Buy/Sell
│ │ ├─ TradeHistory.tsx
│ │ ├─ PositionSummary.tsx
│ │ └─ layout/ (responsive shell: sidebar → mobile stack/menu)
│ └─ src/styles/ # CSS grid, breakpoints 1440 / 768 / 375
├─ tests/
│ └─ TradingPlatform.UnitTests/ # parser, order validator, PnL, reconnect
├─ db/
│ └─ schema.sql # hand-runnable DDL for reviewers
├─ docs/
│ ├─ architecture.md
│ └─ screenshots/ (or demo video)
├─ docker-compose.yml # api + sql server (bonus)
└─ README.md


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