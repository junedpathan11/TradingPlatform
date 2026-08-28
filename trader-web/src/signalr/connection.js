import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

// Backend host from TradingPlatform.Api/Properties/launchSettings.json (Phase 4/5).
const HUB_URL = "https://localhost:7206/hubs/market";

/**
 * Creates a fresh SignalR hub connection to MarketHub (Phase 4).
 * withAutomaticReconnect uses the plan's exact backoff sequence: retry
 * immediately, then after 2s, 5s, 10s. If all four attempts fail, SignalR
 * gives up and fires onclose — the caller (useConnectionStatus) then runs
 * its own manual retry loop on top of this.
 */
export function createMarketConnection() {
  return new HubConnectionBuilder()
    .withUrl(HUB_URL)
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(LogLevel.Information)
    .build();
}