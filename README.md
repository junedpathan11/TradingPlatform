# Real-Time Mini Trading Platform

A candidate evaluation project: a mini real-time trading platform with an
**ASP.NET Core 8 Web API** backend, a **React (Vite, JavaScript)** dashboard,
**SignalR** live price streaming, and **SQL Server** (EF Core 8) trade storage.

> **Status: 🚧 In progress — backend foundation phase.** See
> [`docs/trading-platform-plan.md`](docs/trading-platform-plan.md) for the full roadmap
> and [`AI_HANDOFF.md`](AI_HANDOFF.md) for the current project state.

## What it does (target)

- Authenticates against a provider REST endpoint and connects to its WebSocket price feed
- Streams live prices to the browser (throttled, no page refreshes)
- Accepts Buy/Sell orders executed at the latest live price
- Stores trades in SQL Server and shows history + position/PnL summary
- Fully responsive UI (desktop / tablet / mobile)

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

## Getting started

*Setup instructions will be added as the project takes shape (prerequisites,
configuration, database migration steps, running API + frontend).*

## License

Educational/candidate assignment project.