# Real-Time Mini Trading Platform

A candidate evaluation project implementing a mini real-time trading platform with an **ASP.NET Core 8 Web API** backend, **React + Vite + JavaScript** frontend, **SignalR** live price streaming, and **SQL Server / Entity Framework Core 8** trade storage.

The application supports live/mock market prices, order execution, trade history, positions, PnL calculation, and responsive real-time UI updates without page reloads.

> **Status: ✅ Complete — Phases 0–10**
>
> The core assignment functionality, backend hardening, frontend dashboard, unit tests, documentation, and final functional verification are complete.
>
> See [`docs/trading-platform-plan.md`](docs/trading-platform-plan.md) for the project roadmap and [`AI_HANDOFF.md`](AI_HANDOFF.md) for implementation history and technical decisions.

---

## What it does

- Authenticates with the configured upstream provider API.
- Supports a **Mock** price-feed mode for reliable local demonstration.
- Supports the provider's **Live** WebSocket price feed when valid credentials and connectivity are available.
- Keeps the latest price for each supported symbol in memory.
- Streams price updates to the React frontend through SignalR.
- Throttles/batches market updates to avoid excessive UI/network activity.
- Accepts Buy/Sell orders using the latest available price.
- Validates order symbol, side, and quantity.
- Stores executed trades in SQL Server.
- Displays newest trades immediately after placing an order without reloading the page.
- Calculates positions and realized/unrealized PnL on the backend.
- Refreshes position prices/PnL periodically without a page reload.
- Provides responsive layouts for desktop, tablet, and mobile.
- Provides Swagger/OpenAPI documentation for the backend endpoints.
- Uses structured Serilog console logging.
- Includes automated unit tests for the key backend logic.

---

## Current progress

| Phase | Scope | Status |
|---|---|---|
| 0 | API investigation & assumptions | ✅ Complete |
| 1 | Configuration + EF Core data layer | ✅ Complete |
| 2 | Provider authentication service | ✅ Complete |
| 2.5 | Digest authentication resolution | ✅ Complete |
| 3 | WebSocket price feed + mock fallback | ✅ Complete |
| 4 | SignalR throttled price broadcast | ✅ Complete |
| 5 | REST endpoints + order handling + validation | ✅ Complete |
| 6 | React live dashboard | ✅ Complete |
| 7 | Quick Trade + Trade History + Positions UI | ✅ Complete |
| 8 | Serilog hardening + scaffold cleanup | ✅ Complete |
| 9 | Backend unit tests | ✅ Complete — 23/23 passing |
| 10 | Documentation & delivery review | ✅ Complete |

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 Web API |
| Language | C# |
| Real-time | SignalR |
| Database | SQL Server |
| ORM | Entity Framework Core 8 |
| Validation | FluentValidation |
| Logging | Serilog |
| API documentation | Swagger / Swashbuckle |
| Frontend | React + Vite |
| Frontend language | JavaScript |
| Real-time client | `@microsoft/signalr` |
| Testing | xUnit |
| IDE | Visual Studio 2022 |

> The project intentionally remains on **.NET 8** and the frontend remains **JavaScript**, not TypeScript.

---

# Architecture

```text
┌──────────────────────────────────────────────────────────┐
│                    React + Vite Frontend                 │
│                                                          │
│  Dashboard                                               │
│  ├── Connection Status                                   │
│  ├── Live Prices                                         │
│  ├── Quick Trade                                         │
│  ├── Trade History                                       │
│  └── Positions / PnL                                     │
│                                                          │
│             │ REST                    ▲ SignalR          │
└─────────────┼─────────────────────────┼──────────────────┘
              │                         │
              ▼                         │
┌──────────────────────────────────────────────────────────┐
│                 ASP.NET Core 8 Web API                   │
│                                                          │
│ Controllers                                              │
│ ├── HealthController                                     │
│ ├── PricesController                                     │
│ ├── OrdersController                                     │
│ ├── TradesController                                     │
│ └── PositionsController                                  │
│                                                          │
│ Services                                                 │
│ ├── AuthService                                          │
│ ├── Price Feed Services                                  │
│ ├── PositionCalculator                                   │
│ └── MarketBroadcastService                               │
│                                                          │
│ SignalR                                                  │
│ └── MarketHub                                            │
│                                                          │
│ In-memory latest-price store                             │
│                                                          │
│ EF Core                                                  │
└───────────────────────┬──────────────────────────────────┘
                        │
                        ▼
                ┌───────────────┐
                │   SQL Server  │
                │               │
                │    Trades     │
                └───────────────┘

                    ▲
                    │
             Live WebSocket
                    │
        ┌───────────────────────┐
        │ External Price Feed   │
        └───────────────────────┘