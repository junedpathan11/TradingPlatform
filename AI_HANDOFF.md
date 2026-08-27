# AI_HANDOFF.md — Real-Time Mini Trading Platform (LOCAL PROJECT)

> **⚠️ WORKFLOW MODE (updated 2026-08-27): LOCAL-GUIDED DEVELOPMENT.**
> The user builds **locally in Visual Studio 2022**; the Arena workspace is a **planning/reference workspace only** (frozen reference scaffold + probe evidence — not authoritative). The AI gives small numbered steps; the user performs them locally and reports back; the AI verifies; then next step. The AI must explain every artifact per the learning rule (file name, exact location, why, responsibility, architecture layer, used-by; packages: name, why, phase, required/optional) and must never create/modify anything in the workspace on the user's behalf or advance phases without explicit go-ahead.

**Purpose:** continuity document — any AI agent can resume from this exact point without conversation history.
**Last updated:** 2026-08-27 · **Current phase:** Phase 1 COMPLETE ✅ (locally, verified in SSMS) · **Next phase:** Phase 2 (AuthService) — awaiting explicit user go-ahead

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
6. No credentials/tokens in source or appsettings.json (assignment §15) — user-secrets/env vars when Phase 2 needs them.
7. Do not invent provider API schemas; do not create/use real provider credentials.

## 5. Local environment facts (verified 2026-08-27)

| Item | State |
|---|---|
| IDE | Visual Studio 2022, user's standard .NET workflow |
| Runtime | .NET 8 (user also has .NET 10 — must not be used) |
| SQL Server | **Full SQL Server 2022 instance (16.0.1000.6 RTM), `Server=localhost`, Windows auth** (existing instance, used by user's other projects). LocalDB not required. |
| Connection key | `ConnectionStrings:DefaultConnection` (user's naming choice; code + config consistent) |
| Packages (Api) | EF Core SqlServer 8.0.30, EF Core Design 8.0.30, EF Core Tools 8.0.30, Swashbuckle 6.6.2 (template) |
| Migration workflow | Visual Studio **Package Manager Console** (`Add-Migration`, `Update-Database`, `Script-Migration`), Default project = TradingPlatform.Api |
| Test project | Not created yet (comes with Phase 9 xUnit template step) |

## 6. Local project state (end of Phase 1)

```
C:\Users\patha\source\repos\TradingPlatform\
├─ TradingPlatform.sln
├─ AI_HANDOFF.md, README.md
├─ docs\ (trading-platform-plan.md, assumptions.md, api-investigation.md)
├─ db\schema.sql                          ← pending export via Script-Migration (deliverable)
└─ TradingPlatform.Api\
   ├─ Program.cs                          ← AddDbContext<TradingDbContext> + UseSqlServer(GetConnectionString("DefaultConnection"))
   ├─ appsettings.json                    ← ConnectionStrings:DefaultConnection (localhost, Trusted_Connection, TrustServerCertificate) — no secrets
   ├─ Domain\Trade.cs, TradeSide.cs, TradeStatus.cs
   ├─ Infrastructure\Persistence\TradingDbContext.cs
   ├─ Infrastructure\Persistence\Configurations\TradeConfiguration.cs
   ├─ Migrations\20260827150445_InitialCreate.cs (+ .Designer.cs, TradingDbContextModelSnapshot.cs)
   ├─ Options\                            ← empty for now (AuthApiOptions arrives in Phase 2)
   ├─ Controllers\WeatherForecastController.cs + WeatherForecast.cs  ← template placeholders, replace in Phase 5
   └─ Properties\launchSettings.json
```

## 7. Phase 1 record (COMPLETE locally, 2026-08-27)

**Built:** configuration + EF Core data layer for trade storage. No auth/WS/SignalR/orders/frontend code.

**Database (dbo.Trades, on TradingPlatform @ localhost):**
TradeId INT IDENTITY PK · Symbol NVARCHAR(16) req · Side NVARCHAR(4) CHECK IN ('Buy','Sell') · Quantity DECIMAL(18,2) CHECK > 0 · Price DECIMAL(18,5) · TimestampUtc DATETIME2(3) · Status NVARCHAR(10) CHECK IN ('Filled','Rejected') · IX_Trades_Symbol · IX_Trades_TimestampUtc.

**Decisions taken (local):**
- D5: TradeId = INT IDENTITY in storage; `TRD100xx` display format deferred to Phase 5 DTOs.
- D6 (local variant): **no provider-switch class** (`DatabaseOptions` dropped — real SQL Server locally; workspace variant had Sqlite/InMemory fallbacks that are unnecessary here). `UseSqlServer` hardwired in Program.cs.
- Connection key renamed by user to `DefaultConnection` (consistent across code/config).
- User's Program.cs omits the fail-fast missing-connection-string guard (acceptable; noted style choice).
- Migration `20260827150445_InitialCreate` generated by real tooling and applied via `Update-Database`; **verified in SSMS** (all 7 columns/types correct; 1 history row, ProductVersion 8.0.30). Constraints/indexes guaranteed by the migration transaction.
- Enum→string storage with CHECK constraints matching enum member names exactly (rename tripwire documented in TradeConfiguration).

**Build/test status:** Build succeeded, 0 errors (verified after persistence step). No test project yet (Phase 9).

**Known issues/notes:** ① `db/schema.sql` export pending (Script-Migration → save to `db\schema.sql`). ② WeatherForecast placeholders still present (Phase 5 removes). ③ No .gitignore yet (Phase 10). ④ `Options\` folder empty until Phase 2 (fine — kept from Step 2 skeleton).

## 8. Phase roadmap status

| Phase | Scope | Status |
|---|---|---|
| 0 | Probe APIs + docs | ✅ COMPLETE (probe evidence in docs/api-investigation.md; live re-probe blocked on credentials) |
| 1 | Config + EF Core data layer | ✅ **COMPLETE locally (verified in SSMS)** |
| 2 | AuthService (REST token flow) | ⬜ NEXT — awaiting user go-ahead; live verification blocked on provider credentials (U1/U2) |
| 3 | PriceFeedService (WS + reconnect) | ⬜ live parser blocked on credentials (U4); mock path available |
| 4 | Price store + SignalR throttled broadcast | ⬜ |
| 5 | REST endpoints + order handling (+ remove WeatherForecast) | ⬜ |
| 6 | React live dashboard | ⬜ pre-req: create React+Vite+**JS** app locally (workspace TS scaffold not to be copied) |
| 7 | Order ticket, history, positions UI | ⬜ |
| 8 | Hardening & error handling (Serilog) | ⬜ |
| 9 | Unit tests (xUnit project) | ⬜ |
| 10 | Documentation & delivery (README final, .gitignore, screenshots/demo) | ⬜ |

## 9. Exact next step (requires user go-ahead)

**Phase 2 — AuthService**, step-by-step: ① `AuthApiOptions` (Options/ — BaseUrl + Username/Password bound from config, credentials via user-secrets, NOT appsettings), ② `IAuthService` + `AuthService` (typed HttpClient, POST to `/api/v2/auth/token` per assumption A1, tolerant token parsing per A2, in-memory token cache), ③ DI registration + a temporary verification endpoint or log on startup, ④ build + run check, ⑤ update this handoff. Live success depends on credentials the user must supply; without them the code proceeds against assumptions A1/A2 with failures surfaced clearly.

---
*End of handoff. Update this file at the end of every completed phase. Do not skip §4 rules.*