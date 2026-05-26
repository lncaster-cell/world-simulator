# 19 — Caravan-based trade model

## Status

- MVP caravan-based trade implementation: **Implemented**.
- Version in this PR is intentionally abstract and deterministic.

## Current MVP behavior (implemented)

- Weekly world-level trade pass runs after local city production/consumption.
- Trade uses existing `world.Caravans` and only `IsAvailable` caravans.
- Supported aggregated goods:
  - `Food`
  - `Goods`
  - `Resources`
- Export/import logic:
  - exporter trades only surplus above safe reserve;
  - importer receives only up to target reserve deficit;
  - transfer is clamped by exporter surplus, importer deficit, and caravan capacity.
- Small abstract wealth effect:
  - exporter gets a small positive `Wealth` delta;
  - importer pays a small `Wealth` cost;
  - deltas are clamped to avoid runaway wealth generation.
- Deterministic ordering:
  - settlements and caravans are processed in stable ID order;
  - no randomness is used.

## Simplifications kept in MVP

- Any available caravan may execute trade with any settlement pair in the world.
- No per-item goods or item prices.
- No diplomacy, factions, war, military logistics, or route ownership constraints.

## Route-based delayed shipments

Trade now creates **delayed shipments** instead of instant stock teleport:

- Weekly trade pass creates `TradeShipment` records.
- On departure:
  - exporter stock is reduced immediately;
  - importer pays Wealth immediately;
  - exporter receives Wealth immediately.
- Cargo arrives only on `ArrivalDay` (`DepartureDay + route.TravelDays`).
- Caravan remains occupied while shipment is active and is reusable only after `ReturnDay`.
- Travel timing is authored and deterministic via `TradeRoute.TravelDays` (not map distance calculations).

Current shipment lifecycle:

- `InTransitToDestination` → cargo is traveling to importer.
- `DeliveredReturning` → cargo delivered, caravan is returning.
- `Completed` → caravan can be reused; shipment kept as history.

Future work (not part of this phase):

- robbery/loss/destruction during transit;
- combat/bandits/risk systems;
- distance-based economics and advanced route costs.

## MainlandSupply

`MainlandSupply` remains in the simulation for now and is still active.
It is planned to be replaced/removed in a future phase as caravan trade deepens and becomes the primary external supply mechanism.

## Authored route polylines

- `SettlementMapLocation` remains visual-only placement for settlements on the world map.
- `TradeRoute` is now authoritative route/path data for inter-settlement movement and trade permissions.
- Route lines are authored world data (deterministic presets), not generated pathfinding.
- `TradeRoute.Points` are approximate normalized (0..1) polyline points, easy to edit for road/sea lane shaping.
- Caravan trade now requires an enabled authored `TradeRoute` with matching `CaravanType`.
- The same route data will be reused later for travel, patrols, armies, bandits and route-debug visuals.
- Pathfinding and visual caravan movement along those polylines are future work.

## Caravan hiring economy (city-owned assets)
- Caravans are city-owned economic assets acquired through weekly hiring evaluation.
- Caravan count has no hard cap; practical growth is constrained by city Wealth safety reserve, weekly upkeep affordability, worker budget, and measurable trade demand.
- Land caravans are smaller, cheaper, and route-flexible for land trade pressure.
- Sea caravans are larger and more expensive; they are only eligible for port cities when sea-route demand exists.
- Trade-route travel time stays authored in `TradeRoute.TravelDays` (sea is not automatically faster).
- Risk systems (loss/robbery/destruction) are deferred to future work.
