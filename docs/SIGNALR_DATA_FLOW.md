# SignalR Data Flow

## Overview
Two hubs provide real-time updates:
- `/marketHub` – market prices/stats (no auth attribute today).
- `/botHub` – bot runtime state, logs, and alerts (JWT `[Authorize]`).

## MarketHub
- Groups:
  - `MarketUpdates` (default group used by JoinMarketGroup/LeaveMarketGroup).
  - (No per-symbol grouping implemented yet.)
- Server methods:
  - `JoinMarketGroup()` / `LeaveMarketGroup()`
  - `SendPriceUpdate(Crypto coin)` → broadcast as `ReceivePriceUpdate`
  - `SendMarketStats(MarketStats stats)` → broadcast as `ReceiveMarketStats`
- Producer: `RealtimeBroadcastService` pushes fresh prices/stats via hub context to all/MarketUpdates.

## BotHub
- Attributes: `[Authorize]`
- Groups:
  - `User:{userId}` (auto-added on connect)
  - `Bot:{botId}` via `SubscribeToBot`/`UnsubscribeFromBot`
  - `Strategy:{strategyKey}` via `SubscribeToStrategy`/`UnsubscribeFromStrategy`
- Producers:
  - `BotExecutionHostedService`: strategy runs, order placement, runtime snapshots.
  - `BotMonitorHostedService`: heartbeat/staleness checks, status changes.
  - `BotSignalRDispatcher`: dispatches bot runtime/log/order events to bot/user/strategy groups.

## Auth & connection
- `/botHub` requires JWT; `/marketHub` currently does not. Consider adding `[Authorize]` on MarketHub and claims checks to restrict bot subscriptions to owner.
- Client connects with access token; then subscribes to relevant groups.

## Scale-out
- Currently in-memory only; to run multi-instance, add Redis backplane so broadcasts/groups are shared across nodes.

## Client usage (example)
```ts
// MarketHub
const market = new signalR.HubConnectionBuilder()
  .withUrl("/marketHub")
  .build();
await market.start();
await market.invoke("JoinMarketGroup");
market.on("ReceivePriceUpdate", (payload) => { /* render */ });

// BotHub (requires JWT)
const botHub = new signalR.HubConnectionBuilder()
  .withUrl("/botHub", { accessTokenFactory: () => jwtToken })
  .build();
await botHub.start();
await botHub.invoke("SubscribeToBot", botId);
botHub.on("BotUpdated", (payload) => { /* render bot runtime */ });
```

## Recommendations
- Add `[Authorize]` to `MarketHub` if pricing is user-specific or to prevent abuse.
- Enforce ownership in `SubscribeToBot`/`SubscribeToStrategy` (only owner or admins can join).
- Add message size limits/rate limiting on client invocations.
- Add Redis backplane for scale-out and consistent group delivery.
