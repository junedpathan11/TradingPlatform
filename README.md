Real-Time Mini Trading Platform

A candidate evaluation project implementing a mini real-time trading platform with an ASP.NET Core 8 Web API backend, React + Vite + JavaScript frontend, SignalR live price streaming, and SQL Server / Entity Framework Core 8 trade storage.

The application supports live/mock market prices, order execution, trade history, positions, PnL calculation, and responsive real-time UI updates without page reloads.

Status: ✅ Complete — Phases 0–10

The core assignment functionality, backend hardening, frontend dashboard, unit tests, documentation, and final functional verification are complete.

See docs/trading-platform-plan.md for the project roadmap and AI_HANDOFF.md for implementation history and technical decisions.

What it does

Authenticates with the configured upstream provider API.

Supports a Mock price-feed mode for reliable local demonstration.

Connects to the provider's Live WebSocket feed (Digest auth → token → WS handshake → heartbeat loop), but live market-data frames are not yet flowing — the provider's subscription protocol / quote schema is not publicly documented and stream access is separately requested/gated. See Live price-feed status.

Keeps the latest price for each supported symbol in memory.

Streams price updates to the React frontend through SignalR.

Throttles/batches market updates to avoid excessive UI/network activity.

Accepts Buy/Sell orders using the latest available price.

Validates order symbol, side, and quantity.

Stores executed trades in SQL Server.

Displays newest trades immediately after placing an order without reloading the page.

Calculates positions and realized/unrealized PnL on the backend.

Refreshes position prices/PnL periodically without a page reload.

Provides responsive layouts for desktop, tablet, and mobile.

Provides Swagger/OpenAPI documentation for the backend endpoints.

Uses structured Serilog console logging.

Includes automated unit tests for the key backend logic.

Live price-feed status

Verified Live behavior (2026-08-29):

✅ Digest authentication succeeds.

✅ Token acquisition succeeds.

✅ Live WebSocket connection succeeds (ws://s138.acttrader.com:22138/ws?token=…).

✅ Server heartbeat frames are received (and echoed).

❌ Direct raw WebSocket Test 1 (no subscription) produced only heartbeats.

❌ V1–V5 subscription probes were each tested independently on fresh connections and each produced only heartbeats.

V1 {"action":"subscribe","symbols":[…]} → heartbeats only

V2 {"subscribe":"EURUSD"} → heartbeats only

V3 {"cmd":"subscribe","isin":"EURUSD"} → heartbeats only

V4 {"event":"subscribe","symbols":[…]} → heartbeats only

V5 plain text subscribe EURUSD → heartbeats only

❌ No market-data frame (price / quote / bid / ask / symbol / welcome / error) was received in any test.

⚠️ The provider WebSocket subscription protocol and quote schema are not publicly documented; no official docs, SDKs, or sample clients were found.

⚠️ The provider's API page indicates WebSocket stream access is separately requested/gated (e.g. API-key request via WhatsApp with a verified account).

➡️ Conclusion: Live market-data activation cannot be verified or fixed without provider documentation or granted stream access. The temporary V1–V5 subscribe probes have been removed from the Live feed service; the service now only performs the verified connect + heartbeat + receive loop and continues to log any raw message it receives.

🟢 Mock mode remains the reliable demonstration mode and is fully functional.

Do not claim Live prices are working. Live mode connects and heartbeats, but no provider market-data frames have been observed, so IPriceStore is not populated by the Live feed until the provider protocol/stream access is resolved.

How to run the project

Prerequisites

.NET 8 SDK — for the ASP.NET Core backend.

Node.js 18+ and npm — for the React/Vite frontend.

SQL Server — SQL Server 2019+ / SQL Server LocalDB, or SQL Server via Docker.

Default connection string (TradingPlatform.Api/appsettings.json): Server=localhost;Database=TradingPlatform;Trusted_Connection=True;TrustServerCertificate=True.

Override if needed via environment variable ConnectionStrings__DefaultConnection or user-secrets.

Provider credentials (Live mode only) — AuthApi:Username (user ID), AuthApi:AccountId, and AuthApi:Password in user-secrets only. Never put them in source or appsettings.json.

1. Run the backend (with Swagger)

cd TradingPlatform.Api
dotnet restore
dotnet run

The API listens on http://localhost:5113 and https://localhost:7206 (see Properties/launchSettings.json).
Mock mode is the default (Feed:Mode = Mock in appsettings.json), so no provider credentials are required and the app immediately publishes synthetic ticks.
Swagger UI (Development only): open http://localhost:5113/swagger (or https://localhost:7206/swagger).
Database migration: with the target SQL Server reachable, apply the initial migration (or run schema.sql):

dotnet ef database update

Optional: run the Live provider WebSocket feed
The checked-in config runs Mock so the demo works out of the box. To use the provider path:

dotnet user-secrets init
dotnet user-secrets set "AuthApi:Username" "<provider-user-id>"
dotnet user-secrets set "AuthApi:AccountId" "<provider-account-id>"
dotnet user-secrets set "AuthApi:Password" "<provider-password>"

# Linux/macOS or PowerShell:
export Feed__Mode=Live        # Linux/macOS
$env:Feed__Mode="Live"        # PowerShell
dotnet run

Live mode authenticates, opens the provider WebSocket, and receives heartbeats, but provider market-data frames are not currently flowing — see Live price-feed status. Don't assume Live prices are working.

2. Use Swagger
Start the backend (above), then open http://localhost:5113/swagger.
From the Swagger page you can invoke:
GET /api/health
GET /api/prices
POST /api/orders
GET /api/trades
GET /api/positions
The Swagger OpenAPI JSON is available at http://localhost:5113/swagger/v1/swagger.json.
3. Use Postman
There is no proprietary setup required — the API is plain HTTP JSON + SignalR.

Option A — import the OpenAPI spec: run the backend, then in Postman go to Import → Link and paste http://localhost:5113/swagger/v1/swagger.json (or use the Swagger page's JSON download). Postman generates the collection of REST calls.
Option B — use the assignment Postman collection: if you have the supplied collection, import it and update its base/host variables to http://localhost:5113 (or https://localhost:7206). The backend's own provider endpoints (REST auth + WS feed) stay on the backend — the browser/frontend never calls them directly.
Option C — call endpoints manually:
GET http://localhost:5113/api/health
GET http://localhost:5113/api/prices
POST http://localhost:5113/api/orders with body:

{ "symbol": "EURUSD", "side": "Buy", "quantity": 1 }

GET http://localhost:5113/api/trades
GET http://localhost:5113/api/positions
SignalR hub (live updates): https://localhost:7206/hubs/market
4. Run the React frontend

cd trader-web
npm install
npm run dev

Open http://localhost:5173.

The React app talks to https://localhost:7206 by default (see trader-web/src/api/config.js and trader-web/src/signalr/connection.js).
If you started the API on http://localhost:5113 only, update those two files to http://localhost:5113.
CORS is already configured for http://localhost:5173 and http://localhost:3000 in TradingPlatform.Api/Program.cs.
With the backend in Mock mode you should see prices stream in, buy/sell orders fill, and trade history/positions update live.

5. Run the tests
dotnet build TradingPlatform.sln
dotnet test TradingPlatform.sln

Current progress
Phase	Scope	Status
0	API investigation & assumptions	✅ Complete
1	Configuration + EF Core data layer	✅ Complete
2	Provider authentication service	✅ Complete
2.5	Digest authentication resolution	✅ Complete
3	WebSocket price feed + mock fallback	✅ Complete
4	SignalR throttled price broadcast	✅ Complete
5	REST endpoints + order handling + validation	✅ Complete
6	React live dashboard	✅ Complete
7	Quick Trade + Trade History + Positions UI	✅ Complete
8	Serilog hardening + scaffold cleanup	✅ Complete
9	Backend unit tests	✅ Complete — 23/23 passing
10	Documentation & delivery review	✅ Complete
Tech stack
Layer	Technology
Backend	ASP.NET Core 8 Web API
Language	C#
Real-time	SignalR
Database	SQL Server
ORM	Entity Framework Core 8
Validation	FluentValidation
Logging	Serilog
API documentation	Swagger / Swashbuckle
Frontend	React + Vite
Frontend language	JavaScript
Real-time client	@microsoft/signalr
Testing	xUnit
IDE	Visual Studio 2022
The project intentionally remains on .NET 8 and the frontend remains JavaScript, not TypeScript.

Architecture

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

Screenshots

Desktop Dashboard



Tablet Responsive View



Mobile Responsive View



Security

Never commit provider usernames, passwords, API keys, authentication tokens, or private certificates.

Live provider credentials should be supplied through .NET User Secrets or environment variables.

Do not put real credentials in appsettings.json, source code, README files, or committed Postman collections.

