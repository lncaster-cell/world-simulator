# Rivia Route Network v1

This folder contains the canonical route graph for the Rivia region.

The route durations are fixed constants, not distances calculated from map geometry.

## Files

- `route_nodes.csv` — settlements, ports, piers, region exits, sea exits.
- `route_edges.csv` — confirmed bidirectional routes with fixed durations.
- `route_todo.csv` — known exits/stubs where duration or exact endpoint is not fixed yet.
- `rivia_routes.json` — same data as JSON for the external simulator.

## Core rules

- All confirmed routes are bidirectional.
- `travel_days` is the canonical duration.
- `travel_hours` is `travel_days * 24`.
- `Gotha` is the only full port.
- `Gavern` is a cargo dock / transshipment point.
- `Rivenstal` is a minor receiving pier; ship visits are rare.
- `Tokrus` is a remote eastern island pier.

## Confirmed land routes

| Route | Days |
|---|---:|
| Highrock ↔ Mlynek | 1 |
| Mlynek ↔ Wardmark | 2 |
| Highrock ↔ Wardmark | 3 |
| Wardmark ↔ Rivenstal | 4 |
| Wardmark ↔ Gavern | 6 |
| Wardmark ↔ Brno | 6.5 |
| Rivenstal ↔ Gavern | 2.5 |
| Brno ↔ Gavern | 1 |
| Brno ↔ Rivenstal | 3 |
| Gavern ↔ Wodenz | 5 |

## Confirmed sea routes

| Route | Days | Frequency |
|---|---:|---|
| Rivenstal ↔ Gotha | 3 | rare |
| Gavern ↔ Gotha | 7 | common |
| Gotha ↔ Tokrus | 5.5 | common |
| Rivenstal ↔ Gavern | 3 | rare |

## Not confirmed yet

- Wardmark ↔ West Region Exit duration.
- Wodenz ↔ South Region Exit duration.
- Sea exits north/west/south/east: exact source nodes and durations.
