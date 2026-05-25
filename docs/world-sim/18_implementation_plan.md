# 18 — MVP 0.1 Implementation Plan

## Purpose

This document defines the recommended implementation order for MVP 0.1.

The plan is intentionally incremental. Agents should work through small pull requests instead of attempting the whole application at once.

## Implementation principle

Build the simulator in layers:

```text
1. Project skeleton
2. Core city model
3. Time system
4. Save/Load
5. UI shell
6. Map screen
7. City panel
8. Resource flows
9. Events
10. Logs and debug tools
```

Each layer should be independently reviewable.

## Phase 1 — Technical skeleton

Goal: create a minimal C#/.NET/WPF solution with no simulation complexity.

Expected output:

- solution file;
- WPF application project;
- Core class library project;
- Persistence class library project;
- basic buildable app window;
- no real gameplay logic yet.

Acceptance criteria:

- project builds locally;
- application opens a blank or placeholder window;
- Core and Persistence are separate from UI project.

## Phase 2 — Core city data model

Goal: implement Gotha as data, without complex UI.

Expected output:

- `City` model;
- `CityState` enum;
- Gotha start preset;
- basic validation/clamping for numeric fields.

Acceptance criteria:

- `city_gotha` can be created from a preset;
- start values match documentation;
- no UI-specific logic in Core.

## Phase 3 — Time service

Goal: implement Start/Pause and logical time progression.

Expected output:

- simulation clock;
- day/hour tracking;
- configurable speed;
- lightweight runtime scheduling;
- hourly/daily tick hooks.

Acceptance criteria:

- Start advances time;
- Pause stops time;
- 24 hours advance one day;
- no busy loop is used.

## Phase 4 — Save/Load

Goal: persist MVP state to JSON.

Expected output:

- save model;
- load model;
- local JSON file persistence;
- save/load commands.

Acceptance criteria:

- Gotha state can be saved;
- app can reload the saved state;
- day/hour, city stats, city_state, events/log placeholders are preserved.

## Phase 5 — UI shell

Goal: create the basic app layout.

Expected output:

- main window;
- navigation area;
- placeholder map view;
- placeholder city panel;
- Start/Pause controls.

Acceptance criteria:

- user can see current day/hour;
- user can Start/Pause;
- UI reads state from the simulation layer.

## Phase 6 — Map screen and Gotha card

Goal: implement the MVP navigation flow.

Expected output:

- map image placeholder;
- clickable Gotha hotspot;
- city card;
- Open City button.

Acceptance criteria:

- clicking Gotha opens a card;
- card shows factual state only;
- Open City opens full city panel;
- no routes/armies/caravans are added.

## Phase 7 — City panel

Goal: display Gotha details.

Expected sections:

- Overview;
- Stocks;
- Flows;
- Events;
- Journal;
- Debug.

Acceptance criteria:

- all required sections exist;
- values are bound to current simulation state;
- debug remains clearly separated from normal UI.

## Phase 8 — Resource flow model

Goal: implement first real daily calculations.

Expected output:

- food consumption;
- placeholder fishing/hunting/mainland supply values;
- daily food balance;
- technical log entry for daily food change.

Acceptance criteria:

- food decreases/increases by logged daily components;
- daily consumption for population 420 equals 84;
- calculations are visible in technical log.

## Phase 9 — Event model

Goal: implement initial event structure and a small event set.

Expected output:

- event model;
- active/completed events;
- duration;
- cooldown;
- manual debug trigger;
- game journal entries.

Acceptance criteria:

- events are concrete incidents, not parameter labels;
- manual event trigger works from debug panel;
- event effects are logged.

## Phase 10 — City state rules

Goal: implement stable city_state transitions.

Expected output:

- city_state calculation rules;
- entry thresholds;
- exit thresholds;
- minimum duration/cooldown rule if needed.

Acceptance criteria:

- city_state does not flip from small daily noise;
- stagnation is the default start state;
- UI shows factual current state only.

## Phase 11 — Polish and documentation sync

Goal: align implementation and docs.

Expected output:

- update docs where implementation decisions changed;
- verify README remains accurate;
- add developer run instructions.

Acceptance criteria:

- docs match application behavior;
- no out-of-scope systems are present.

## Explicit implementation bans for MVP 0.1

Do not implement:

- multiple cities;
- caravans;
- armies;
- clans;
- states;
- wars;
- NWN2 runtime integration;
- Unity;
- detailed infrastructure/building simulation;
- cloud services;
- database persistence.
