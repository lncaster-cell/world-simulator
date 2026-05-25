# 19 — Codex Task Plan

## Purpose

This document converts the MVP 0.1 implementation plan into small Codex-friendly tasks.

Each task should normally become a separate branch and pull request.

## General Codex rules

For every task:

- read all `docs/world-sim/*.md` files first;
- do not expand MVP scope;
- do not add Unity;
- do not add NWN2 runtime integration;
- do not add multiple cities;
- keep changes small and reviewable;
- update documentation when implementation decisions change.

## Task 01 — Create C# WPF solution skeleton

### Goal

Create the initial buildable solution.

### Scope

- C#/.NET solution.
- WPF app project.
- Core class library.
- Persistence class library.
- Placeholder main window.

### Out of scope

- real simulation logic;
- events;
- map image work;
- save/load implementation.

### Acceptance criteria

- solution builds;
- app opens;
- no out-of-scope systems are added.

## Task 02 — Add Gotha core model and preset

### Goal

Implement the city model and Gotha start preset.

### Scope

- `City` model;
- `CityState` enum;
- `city_gotha` preset;
- basic numeric clamping.

### Acceptance criteria

- Gotha starts with documented values;
- Core project has no UI dependency.

## Task 03 — Add simulation clock

### Goal

Implement Start/Pause and day/hour progression.

### Scope

- simulation clock service;
- configurable speed;
- hourly tick hook;
- daily tick hook;
- no busy loop.

### Acceptance criteria

- Start advances time;
- Pause stops time;
- 24 hours produce one day;
- runtime loop remains lightweight.

## Task 04 — Add JSON Save/Load

### Goal

Persist current MVP state.

### Scope

- save file model;
- save command;
- load command;
- local JSON file.

### Acceptance criteria

- saved state reloads correctly;
- day/hour and city values persist.

## Task 05 — Add UI shell and basic binding

### Goal

Display current simulation state in the app.

### Scope

- main layout;
- Start/Pause controls;
- day/hour display;
- Gotha overview placeholder.

### Acceptance criteria

- UI shows current day/hour;
- Start/Pause works through the UI;
- UI reads from the simulation layer.

## Task 06 — Add map screen and Gotha card

### Goal

Implement the MVP navigation flow.

### Scope

- map placeholder;
- Gotha hotspot;
- city card;
- Open City button.

### Acceptance criteria

- clicking Gotha opens card;
- card shows factual state only;
- no forecasts/risks are shown.

## Task 07 — Add city panel sections

### Goal

Create the full city panel structure.

### Scope

- Overview section;
- Stocks section;
- Flows section;
- Events section;
- Journal section;
- Debug section.

### Acceptance criteria

- sections exist;
- debug is clearly separated from normal view.

## Task 08 — Add daily food flow

### Goal

Implement first real city calculation.

### Scope

- daily food consumption;
- fishing/hunting/mainland supply placeholders;
- daily balance;
- technical log entry.

### Acceptance criteria

- population 420 consumes 84 food/day;
- daily balance is visible in technical log.

## Task 09 — Add event system skeleton

### Goal

Implement the structure for concrete incidents.

### Scope

- event model;
- active/completed lists;
- duration;
- cooldown;
- debug manual trigger.

### Acceptance criteria

- events are concrete incidents;
- manual trigger works;
- event effects can be logged.

## Task 10 — Add city_state transition rules

### Goal

Make city_state stable and non-noisy.

### Scope

- basic thresholds;
- entry/exit conditions;
- recovery path;
- no risk forecast UI.

### Acceptance criteria

- city_state does not flip from small daily changes;
- UI displays current factual state only.

## Task 11 — Documentation and developer instructions

### Goal

Keep repo usable.

### Scope

- run instructions;
- build instructions;
- architecture updates;
- implementation notes.

### Acceptance criteria

- new developer/agent can build and understand the app;
- docs match implementation.
