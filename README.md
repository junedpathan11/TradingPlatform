# Real-Time Mini Trading Platform

A candidate evaluation project: a mini real-time trading platform with an
**ASP.NET Core 8 Web API** backend, a **React (Vite, JavaScript)** dashboard,
**SignalR** live price streaming, and **SQL Server** (EF Core 8) trade storage.

> **Status: 🚧 In progress — Phases 0–2 complete (backend foundation + provider auth).**
> Phase 3 (WebSocket price feed) is on hold pending provider credentials.
> See [`docs/trading-platform-plan.md`](docs/trading-platform-plan.md) for the full roadmap
> and [`AI_HANDOFF.md`](AI_HANDOFF.md) for the current project state.

## What it does (target)

- Authenticates against a provider REST endpoint and connects to its WebSocket price feed
- Streams live prices to the browser (throttled, no page refreshes)
- Accepts Buy/Sell orders executed at the latest live price
- Stores trades in SQL Server and shows history + position/PnL summary
- Fully responsive UI (desktop / tablet / mobile)

## Progress

| Phase | Scope | Status |
|---|---|---|
| 0 | API investigation & assumptions | ✅ Done |
| 1 | Configuration + EF Core data layer (`Trades` table, migration applied) | ✅ Done |
| 2 | Provider authentication service (`AuthService`, verified live to the 401 round trip) | ✅ Done |
| 3 | WebSocket price feed (dual-mode: live + mock) | ⏸️ On hold — provider credentials not provided in the assignment PDF |
| 4–10 | SignalR streaming, REST endpoints, React dashboard, hardening, tests, delivery | ⬜ Upcoming |

## Tech stack (locked)

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8, C# |
| Real-time | SignalR |
| Data | Entity Framework Core 8, SQL Server |
| Frontend | React + Vite + JavaScript (not TypeScript) |
| Tests | xUnit |

## Documentation

| File | Purpose |
|---|---|
| [`docs/trading-platform-plan.md`](docs/trading-platform-plan.md) | Master roadmap (phases, architecture, priorities) |
| [`AI_HANDOFF.md`](AI_HANDOFF.md) | Project continuity document (current phase, decisions, status) |
| [`docs/assumptions.md`](docs/assumptions.md) | Assumptions & confirmed/unknown provider API details |
| [`docs/api-investigation.md`](docs/api-investigation.md) | Provider endpoint investigation evidence |
| [`db/schema.sql`](db/schema.sql) | SQL Server schema (mirrors the EF Core migration) |

## Getting started

### Prerequisites
- Visual Studio 2022 with the **.NET 8** SDK
- SQL Server (LocalDB, Express, or full — any works; connection string is config-driven)

### Run the API
1. Open `TradingPlatform.sln` in Visual Studio 2022.
2. Set the connection string in `TradingPlatform.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`) if yours differs.
3. Create/update the database: **Tools → NuGet Package Manager → Package Manager Console** → `Update-Database` (Default project: `TradingPlatform.Api`).
4. Press **F5** — Swagger UI opens.
5. (Optional) Provider credentials, if you have them: right-click `TradingPlatform.Api` → **Manage User Secrets** → set `AuthApi:Username` / `AuthApi:Password`.

> **Note:** the assignment PDF does not include provider credentials, so live price streaming
> is not verifiable end-to-end yet. The authentication service is fully implemented and
> verified up to the provider's response; a dual-mode (mock/live) price feed is planned
> so the platform is fully demoable regardless. This is disclosed per assignment §15
> ("mention any API assumptions").

*Full setup instructions (frontend, seeding, Docker) will be added as the project takes shape.*

## License

Educational/candidate assignment project.