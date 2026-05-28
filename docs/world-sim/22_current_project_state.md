# 22 — Current Project State

## Purpose

This document records the current factual state of the repository.

Use it to resolve conflicts between older design notes and the implemented project.

## Current implementation

The repository currently contains a working C#/.NET/WPF external world simulator for the NWN2 module.

Implemented areas:

- multi-city world state;
- Rivia region data;
- settlement map locations;
- city economy profiles;
- daily city simulation;
- food, wealth, crime, production, and consumption calculations;
- city events;
- caravans;
- trade routes;
- trade shipments;
- route path loading and visualization;
- route path authoring helpers;
- simulation clock;
- JSON save/load;
- WPF UI;
- Core and Persistence tests.

## Current source-of-truth order

When documents disagree, prefer sources in this order:

1. `README.md`
2. this document
3. tests in `tests/`
4. implementation in `src/`
5. older design documents in `docs/world-sim/`

Older documents may describe historical MVP constraints. They should not be treated as current blockers when the corresponding system already exists in code.

## Historical MVP constraints

Some older documents were written when the project was a docs-only or single-city MVP.

Historical constraints such as "one city only" or "no caravans" are not current project rules anymore, because the current code already includes multiple cities, caravans, trade routes, and route path tooling.

These older documents remain useful as design history, but not as the current implementation contract.

## Current stabilization priorities

Before adding new large systems, stabilize the existing simulator:

1. keep documentation aligned with the current implementation;
2. keep build/test automation reliable;
3. verify per-city event behavior in UI, simulation, save, and load;
4. reduce the size and responsibility of `MainWindowViewModel`;
5. split trade flow logic into smaller services;
6. split save/load mapping, migration, and validation into smaller components;
7. add city-state hysteresis/state memory before making city states more complex.

## Expansion notes

The following systems are not implemented as current production features. This list is a planning aid, not a prohibition for agents; if the user's request requires one of these areas, agents may implement it while updating the relevant design and tests:

- clans;
- dynasties;
- states;
- wars;
- diplomacy;
- armies;
- NWN2 runtime integration;
- Unity integration;
- database or cloud persistence;
- full character/family simulation.
