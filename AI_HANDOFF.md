# AI_HANDOFF.md — Real-Time Mini Trading Platform (LOCAL PROJECT)

> **⚠️ WORKFLOW MODE (updated 2026-08-27): LOCAL-GUIDED DEVELOPMENT.**
> The user builds **locally in Visual Studio 2022**; the Arena workspace is a **planning/reference workspace only** (frozen reference scaffold + probe evidence — not authoritative). The AI gives small numbered steps; the user performs them locally and reports back; the AI verifies; then next step. The AI must explain every artifact per the learning rule (file name, exact location, why, responsibility, architecture layer, used-by; packages: name, why, phase, required/optional) and must never create/modify anything in the workspace on the user's behalf or advance phases without explicit go-ahead.

**Purpose:** continuity document — any AI agent can resume from this exact point without conversation history.
**Last updated:** 2026-08-27 · **Current phase:** Phase 2 COMPLETE ✅ (locally, live-verified) · **Phase 3: ON HOLD (user decision)** · **Next when resumed:** Phase 3 (PriceFeedService, dual-mode)

---

## 1. Project mission

Build a **Real-Time Mini Trading Platform** (candidate assignment): an ASP.NET Core backend authenticates against a provider REST endpoint to get a token, connects to the provider WebSocket price feed, keeps latest price per symbol in memory, streams throttled updates to a React dashboard via SignalR, accepts Buy/Sell orders executed at the live price, stores trades in SQL Server, and updates history/positions without page reloads. Mobile-responsive UI required (1440 / 768 / 375 px).
**Time budget:** 6–10 hours (clean working prototype over over-engineering).

## 2. Project documents (local, solution-level)

| File | Purpose |
|---|---|
| HR assignment PDF (5 pages) | Source of truth — requirements, endpoints, evaluation weights |
| `docs/trading-platform-plan.md` | Master roadmap — do not change without explicit decision |
| `docs/assumptions.md` | Living assumptions register (A/B/C/D sections) |
| `docs/api-investigation.md` | Provider endpoint probe evidence (update only with new evidence) |
| `README.md` | Human/GitHub-facing doc — grows progressively, completed in Phase 10 |
| `AI_HANDOFF.md` | This file — update after every completed phase |

## 3. LOCKED technology stack (do not substitute)

ASP.NET Core 8 (`net8.0`, do NOT retarget) · C# · SignalR (built into the framework; npm client later) · EF Core 8 (8.0.30) · SQL Server · React + Vite + **JavaScript (NOT TypeScript)** · xUnit.

## 4. Rules of engagement (binding)

1. No .NET version or NuGet version changes without explicit user instruction.
2. No TypeScript anywhere; frontend stays JavaScript.
3. No unnecessary libraries; nothing installed without stating why/phase/required-optional.
4. Step-by-step only: instruct → user performs → verify → next. Never auto-advance a phase; STOP after each.
5. Learning rule always applies (see workflow header).
6. No credentials/tokens in source or appsettings.json (assignment §15) — user-secrets/env vars (user-secrets already enabled for the Api project).
7. Do not invent provider API schemas; do not create/use real provider credentials.

## 5. Local environment facts (verified 2026-08-27)

| Item | State |
|---|---|
| IDE | Visual Studio 2022, user's standard .NET workflow |
| Runtime | .NET 8 (user also has .NET 10 — must not be used) |
| SQL Server | Full SQL Server 2022 instance (16.0.1000.6 RTM), `Server=localhost`, Windows auth (existing instance; SSMS in use) |
| App URLs (dev) | `https://localhost:7206` (Swagger), `http://localhost:5113` |
| Connection key | `ConnectionStrings:DefaultConnection` (user's naming choice; code + config consistent) |
| Packages (Api) | EF Core SqlServer 8.0.30, EF Core Design 8.0.30, EF Core Tools 8.0.30, Swashbuckle 6.6.2 (template) |
| Migration workflow | Visual Studio **Package Manager Console** (`Add-Migration`, `Update-Database`, `Script-Migration`), Default project = TradingPlatform.Api |
| Git | Local repo on `main` (VS 2022 Git integration); commit checkpoint at each phase boundary |
| Provider reachability | ✅ Confirmed from user's local network (2026-08-27, live POST via AuthService → provider returned 401 to placeholder creds) |
| Provider credentials | ❌ Assignment PDF contains NONE (user re-confirmed 2026-08-27). Candidate may request from issuer. All live-flow unknowns U1–U6 blocked on this. |
| Test project | Not created yet (comes with Phase 9 xUnit template step) |

## 6. Local project state (end of Phase 2)

```
C:\Users\patha\source\repos\TradingPlatform\
├─ TradingPlatform.sln
├─ AI_HANDOFF.md, README.md
├─ docs\ (trading-platform-plan.md, assumptions.md, api-investigation.md)
├─ db\schema.sql                          ← exported via Script-Migration (Phase 10 deliverable, done early)
└─ TradingPlatform.Api\
   ├─ Program.cs                          ← AddDbContext + UseSqlServer + Configure<AuthApiOptions> + AddHttpClient<IAuthService,AuthService>
   ├─ appsettings.json                    ← ConnectionStrings:DefaultConnection + AuthApi section (BaseUrl/TokenPath — non-secret)
   ├─ appsettings.Development.json        ← template
   ├─ UserSecretsId in .csproj            ← secrets.json holds AuthApi:Username/Password (outside repo)
   ├─ Domain\Trade.cs, TradeSide.cs, TradeStatus.cs
   ├─ Exceptions\AuthException.cs         ← Phase 2
   ├─ Infrastructure\
   │  ├─ Persistence\TradingDbContext.cs + Configurations\TradeConfiguration.cs
   │  └─ Services\IAuthService.cs, AuthService.cs   ← Phase 2
   ├─ Migrations\20260827150445_InitialCreate.cs (+ .Designer.cs, TradingDbContextModelSnapshot.cs)
   ├─ Options\AuthApiOptions.cs           ← Phase 2
   ├─ Controllers\WeatherForecastController.cs + WeatherForecast.cs  ← template placeholders, replace in Phase 5
   ├─ Controllers\AuthProbeController.cs  ← TEMPORARY Phase 2 verification endpoint, remove in Phase 5
   └─ Properties\launchSettings.json
```

## 7. Phase 1 record (COMPLETE locally, committed 2026-08-27)

**Built:** configuration + EF Core data layer for trade storage.

**Database (dbo.Trades on TradingPlatform @ localhost):**
TradeId INT IDENTITY PK · Symbol NVARCHAR(16) req · Side NVARCHAR(4) CHECK IN ('Buy','Sell') · Quantity DECIMAL(18,2) CHECK > 0 · Price DECIMAL(18,5) · TimestampUtc DATETIME2(3) · Status NVARCHAR(10) CHECK IN ('Filled','Rejected') · IX_Trades_Symbol · IX_Trades_TimestampUtc.

**Decisions:** D5 TradeId = INT IDENTITY in storage (`TRD100xx` display id deferred to Phase 5 DTOs) · D6-local **no provider-switch class** (`UseSqlServer` hardwired; Sqlite/InMemory packages deliberately not installed) · connection key renamed by user to `DefaultConnection` · migration `20260827150445_InitialCreate` generated by real tooling, applied via `Update-Database`, verified in SSMS (columns/types/history row 8.0.30) · enum→string storage with CHECK constraints matching enum names exactly (rename tripwire).

**Build/test:** build 0 errors; no test project yet (Phase 9). `db/schema.sql` exported (Solution Items).

## 7b. Phase 2 record (COMPLETE locally, live-verified 2026-08-27)

**Built:** provider authentication flow. No WebSocket/SignalR/orders/frontend code.

**Files created:**
- `Options/AuthApiOptions.cs` — bound to "AuthApi" section; BaseUrl/TokenPath in appsettings.json; Username/Password in user-secrets ONLY
- `Exceptions/AuthException.cs` — message + provider HTTP status
- `Infrastructure/Services/IAuthService.cs` — `GetTokenAsync` / `InvalidateToken` contract
- `Infrastructure/Services/AuthService.cs` — typed HttpClient POST per A1 (`{"username","password"}`); tolerant parsing per A2 (token/accessToken/access_token, root or "data" wrapper, case-insensitive); recognizes the confirmed `{"success":false,...}` envelope; in-memory token cache (lock-protected, expiresIn−60s margin, no-expiry cache when provider states none); `InvalidateToken()` = Phase 3 WS-401 re-auth hook; never logs password/token (length only)
- `Controllers/AuthProbeController.cs` — TEMPORARY GET /api/authprobe verification endpoint (remove in Phase 5); auth failures → 502 JSON

**Modified:** `Program.cs` (Configure&lt;AuthApiOptions&gt; + `AddHttpClient<IAuthService, AuthService>` with BaseAddress + 15 s timeout; temporary startup binding smoke-log), `appsettings.json` (AuthApi section), `.csproj` (UserSecretsId via VS).

**Decisions:** HTTP-status check precedes envelope parsing · 502 semantics = upstream auth failure · cache without expiry when provider states no lifetime · temporary Console.WriteLine binding smoke-test (remove Phase 8).

**Verification:** build 0 errors (one dev-time fix: 2× CS1503 — missing `.GetString()` on JsonElement) · startup line `AuthApi bound: … Username=set, Password=set` · live probe with placeholder creds → provider **HTTP 401** → clean **502** JSON `{"result":"auth failed","error":"Provider auth failed with HTTP 401."}`. Proves config binding, typed client, provider reachable from local network, clean error path. 200/success path unverified (no credentials).

## 8. Phase roadmap status

| Phase | Scope | Status |
|---|---|---|
| 0 | Probe APIs + docs | ✅ COMPLETE (evidence in docs/api-investigation.md; live re-probe blocked on credentials) |
| 1 | Config + EF Core data layer | ✅ COMPLETE locally (verified in SSMS, committed) |
| 2 | AuthService (REST token flow) | ✅ COMPLETE locally (live-verified 401 round trip; success path pending credentials U1/U2) |
| 3 | PriceFeedService (WS + reconnect) | ⏸️ **ON HOLD (user decision 2026-08-27).** Agreed approach when resumed: dual-mode — `Feed:Mode` config (Mock/Live), `MockPriceFeedService` + real `LivePriceFeedService` through the same `IPriceStore` pipeline, zero downstream changes; disclose mock in final delivery |
| 4 | Price store + SignalR throttled broadcast | ⬜ |
| 5 | REST endpoints + order handling (+ remove WeatherForecast + AuthProbe) | ⬜ |
| 6 | React live dashboard | ⬜ pre-req: create React + Vite + **JavaScript** app locally (fresh scaffold; NOT the workspace TS one) |
| 7 | Order ticket, history, positions UI | ⬜ |
| 8 | Hardening & error handling (Serilog; remove temporary smoke logs) | ⬜ |
| 9 | Unit tests (xUnit project) | ⬜ |
| 10 | Documentation & delivery (README final, .gitignore review, screenshots/demo) | ⬜ |

## 9. Exact next steps / decision log

**Decision log (2026-08-27):** Confirmed the assignment PDF provides no provider credentials (matches api-investigation §5). User put Phase 3 ON HOLD. Agreed future approach when resumed: dual-mode feed — `Feed:Mode` config (Mock/Live), `MockPriceFeedService` + real `LivePriceFeedService` behind the same pipeline, zero downstream changes; disclose mock in final delivery. Candidate may also request credentials from the assignment issuer; if supplied, no code changes needed — real creds go into `secrets.json` (`AuthApi:Username/Password`) and the probe should return 200.

**When Phase 3 resumes, step order:** ① `FeedOptions` (WS URL, reconnect delays, Mode) · ② `IPriceStore`/`InMemoryPriceStore` + `PriceTick` · ③ `PriceMessageParser` (tolerant; final shape waits on U4) · ④ `LivePriceFeedService : BackgroundService` (connect `ws://…/ws?token=…`, receive loop, exponential backoff, re-auth on 401 via `InvalidateToken()`) · ⑤ `MockPriceFeedService` (random-walk FX ticks, same pipeline) · ⑥ `FeedStateService` (Disconnected/Connecting/Connected/Error) + health/probe surfacing · ⑦ build/run verification · ⑧ handoff update.

---
*End of handoff. Update this file at the end of every completed phase. Do not skip §4 rules.*