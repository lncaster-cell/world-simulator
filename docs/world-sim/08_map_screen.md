# 08 — Map Screen

## Role

Map is the main screen.

## MVP interactions

- Gotha is clickable.
- Click opens city card.
- City card contains “Open city” button.
- “Open city” opens full city panel.

## Not in MVP map

No simulation of:

- routes;
- armies;
- caravans;
- influence zones;
- states/factions layer.

## Map asset (MVP)

- MVP map asset is stored at `src/WorldSimulator.App/Assets/Maps/world_map.png`.
- At this stage this file is only a static asset (no UI binding yet).
- Map rendering, clickability, and interactions are planned for Task 06.
