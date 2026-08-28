# AI_HANDOFF.md — Real-Time Mini Trading Platform (LOCAL PROJECT)

> **⚠️ WORKFLOW MODE (updated 2026-08-27): LOCAL-GUIDED DEVELOPMENT.**
> The user builds **locally in Visual Studio 2022**; the Arena workspace is a **planning/reference workspace only** (frozen reference scaffold + probe evidence — not authoritative). The AI gives small numbered steps; the user performs them locally and reports back; the AI verifies; then next step. The AI must explain every artifact per the learning rule (file name, exact location, why, responsibility, architecture layer, used-by; packages: name, why, phase, required/optional) and must never create/modify anything in the workspace on the user's behalf or advance phases without explicit go-ahead.

**Purpose:** continuity document — any AI agent can resume from this exact point without conversation history.
**Last updated:** 2026-08-28 · **Current phase:** Phase 2.5 COMPLETE ✅ (AUTH SOLVED, live-verified) · **Phase 3 IN PROGRESS** (Step 12 issued: feed config + price store; Steps 13–15 follow) 

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

```
C:\Users\patha\source\repos\TradingPlatform\
├─ TradingPlatform.sln
├─ AI_HANDOFF.md, README.md
├─ docs\ (trading-platform-plan.md, assumptions.md, api-investigation.md)
├─ db\schema.sql
└─ TradingPlatform.Api\
   ├─ Program.cs                       ← AddDbContext; Configure<AuthApiOptions>; AddHttpClient<IAuthService,AuthService>
   │                                      (BaseAddress+15s+default User-Agent; NO handler credentials); temp startup smoke-log
   ├─ appsettings.json                 ← ConnectionStrings:DefaultConnection; AuthApi (BaseUrl/TokenPath); [Feed section = Step 12, pending]
   ├─ Domain\Trade.cs, TradeSide.cs, TradeStatus.cs
   ├─ Exceptions\AuthException.cs
   ├─ Infrastructure\Persistence\TradingDbContext.cs (+ Configurations\TradeConfiguration.cs)
   ├─ Infrastructure\Services\IAuthService.cs, AuthService.cs
   │                                      ← Phase 2.5: MANUAL MD5 Digest handshake (challenge → answer),
   │                                        TryAddWithoutValidation raw header, {} bodies, tolerant parser
   │                                        incl. "result" token field; token cache + InvalidateToken()
   ├─ Migrations\20260827150445_InitialCreate.cs (+ Designer, Snapshot)
   ├─ Options\AuthApiOptions.cs        ← Username/AccountId/Password/BaseUrl/TokenPath
   ├─ Controllers\WeatherForecastController.cs, WeatherForecast.cs  ← placeholders (Phase 5 removal)
   ├─ Controllers\AuthProbeController.cs ← TEMPORARY auth verification (Phase 5 removal)
   └─ Properties\launchSettings.json
```

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

## 8. Phase roadmap status

| Phase | Scope | Status |
|---|---|---|
| 0 | Probe APIs + docs | ✅ COMPLETE |
| 1 | Config + EF Core data layer | ✅ COMPLETE (SSMS-verified, committed) |
| 2 | AuthService foundation | ✅ COMPLETE (committed) |
| 2.5 | Auth resolution (Digest reverse-engineering) | ✅ **COMPLETE — token acquisition live-verified** |
| 3 | Price feed (live WS + mock fallback) | 🔄 **IN PROGRESS** — Step 12 issued (FeedOptions, PriceTick, IPriceStore/InMemoryPriceStore, appsettings Feed, DI). Next: Step 13 FeedStateService + PriceMessageParser · Step 14 LivePriceFeedService (+ Mock) · Step 15 first live ticks + raw-message capture (closes U4) |
| 4 | SignalR throttled broadcast | ⬜ |
| 5 | REST endpoints + orders (+ remove WeatherForecast + AuthProbe) | ⬜ |
| 6 | React live dashboard | ⬜ pre-req: fresh React+Vite+**JS** scaffold locally (never the workspace TS one) |
| 7 | Order ticket, history, positions UI | ⬜ |
| 8 | Hardening (Serilog; remove temp logs/probe) | ⬜ |
| 9 | Unit tests (xUnit project) | ⬜ |
| 10 | Docs & delivery (README final, .gitignore review, screenshots/demo) | ⬜ |

## 9. Decision log / exact next steps

**2026-08-27:** Phase 3 held (no credentials). **2026-08-28:** credentials received → auth investigation ran (T0–T2 body shapes → OPTIONS probe revealed Digest → MD5 bisection → UA discovery → `result` field) → **auth solved**. User approved live-first Phase 3 with mock fallback behind `Feed:Mode`.

**Immediate next actions:**
1. User confirms Step 12 files created + build green (then commit Step 12–15 together or per step, user's choice).
2. Agent issues Step 13: `FeedStateService` (connection state singleton: Disconnected/Connecting/Connected/Error) + `PriceMessageParser` (tolerant, logs-and-skips malformed; final field mapping waits for live capture).
3. Step 14: `ILivePriceFeedService`/`LivePriceFeedService` (`BackgroundService`; connect `ws://…/ws?token={token}` via `IAuthService.GetTokenAsync`; receive loop; exponential backoff w/ jitter; on WS 401 → `InvalidateToken()` → re-auth) + `MockPriceFeedService` (random-walk FX ticks) + mode selection in DI.
4. Step 15: run → observe console → capture 2–3 raw WS messages verbatim → finalize parser field mapping → close U4/U5; update docs; commit.

---
*End of handoff. Update at the end of every completed phase (and major step batches). Do not skip §4 rules.*