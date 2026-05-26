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
