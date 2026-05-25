# 16 — Architecture Decisions

## Purpose

This document records early technical architecture decisions for the external NWN2 world simulator.

The goal is to make the future implementation predictable for agents/Codex while keeping MVP 0.1 small and controlled.

## ADR-001: Build a standalone Windows desktop application

### Decision

MVP 0.1 will be implemented as a standalone Windows desktop application.

### Rationale

- The simulator is intended to run outside NWN2.
- MVP 0.1 needs map navigation, city panels, logs, save/load, and debug tools.
- A desktop app is simpler than embedding this into NWN2 or building a game engine layer.

### Consequences

- No Unity project is created.
- No NWN2 runtime integration is implemented in MVP 0.1.
- The simulator must remain useful as a standalone tool.

## ADR-002: Keep simulation core separate from UI

### Decision

The simulation logic must be separated from the visual UI layer.

### Intended structure

```text
WorldSimulator.App        -> desktop UI shell
WorldSimulator.Core       -> simulation rules and domain model
WorldSimulator.Persistence -> save/load and file formats
```

### Rationale

The UI should display state and send user commands. It should not own the simulation rules.

This prevents the project from becoming a UI-script mess and makes future tests and NWN2 export easier.

## ADR-003: Use fixed logical ticks with lightweight runtime scheduling

### Decision

The simulator must use a lightweight runtime loop and fixed logical ticks.

### Rule

The app must not wait until an hourly/daily boundary and then calculate everything in one heavy burst.

Instead, it should:

- track elapsed real time continuously;
- prepare pending balances and timers gradually;
- mutate city state only on fixed logical ticks.

### Tick levels

- Runtime loop: lightweight scheduling and elapsed-time tracking.
- Hourly tick: minor simulation step.
- Daily tick: main economy, resource, and city_state step.

### Performance constraint

Do not implement a busy loop. The runtime loop must be CPU-friendly.

## ADR-004: Save files use JSON for MVP

### Decision

MVP 0.1 save/load will use local JSON files.

### Rationale

- Easy to inspect manually.
- Easy for agents to generate and debug.
- Good enough for one-city MVP.
- Avoids premature database complexity.

### Future note

A database can be considered later if multi-city, long-running history, or NWN2 synchronization requires it.

## ADR-005: Use stable IDs for future integration

### Decision

All durable entities must have stable IDs.

For MVP 0.1:

```text
city_gotha
```

### Rationale

Stable IDs prevent later breakage when display names, UI labels, or translations change.

## ADR-006: Do not encode NWN2-specific objects in the core model

### Decision

The simulation core must not store NWN2-specific tags, area resrefs, object references, or script names.

### Rationale

The simulator must remain independent from NWN2. Future integration should be handled by a separate adapter/export layer.

## ADR-007: Documentation before implementation

### Decision

For MVP 0.1, implementation work should follow documented tasks and should not expand scope ad hoc.

### Rationale

The project is design-heavy and simulation-heavy. Uncontrolled implementation will quickly produce scope creep.

## ADR-008: Long-running simulation safety

### Decision

The simulator must enforce automatic retention limits for runtime history data.

### Rule

- Technical UI log is bounded (latest 500 entries).
- Completed events history is bounded in Core (latest 100 events).
- Retention is automatic; manual cleanup is not the primary stability mechanism.
- Save files persist only already-trimmed runtime history.

### Rationale

The simulator is expected to run for long sessions. Unlimited growth of logs/history degrades UI responsiveness and inflates save files.

### Consequences

- Core owns completed-events lifecycle and trimming policy.
- App owns technical-log UI retention.
- Future history-like systems must define explicit retention policies.

## ADR-009: Daily aggregated population change for MVP

### Decision

Population in MVP changes once per in-game day as an aggregated delta.

### Rule

- No separate birth/death entities are tracked.
- No family, age, or migration simulation is added.
- The daily population delta depends on `CityState` and base city conditions (food, mood, security).

### Rationale

This keeps the MVP simulation deterministic, readable in logs, and lightweight while still reflecting city decline and growth.

Семантика и приоритеты `CityState` описаны в `docs/world-sim/12_city_states.md`.

## ADR-010: City-scoped journals

### Decision

Пользовательский журнал симуляции должен быть city-scoped: одна летопись на один город.

### Rule

- Каждая запись пользовательской летописи хранит `CityId` и `CityName`.
- Текущая UI-летопись показывает записи только выбранного города.
- Глобальные события мира не смешиваются с city-level дневной летописью.

### MVP 0.1 scope

- Реализован только один city-scoped журнал для `CityId = "gotha"`.
- Отдельная летопись мира — future feature и не реализуется в этом этапе.

### Rationale

Смешивание всех городов в одном пользовательском журнале делает диагностику состояния конкретного города нечитаемой и мешает анализу по дням.

### Consequences

- Multi-city расширение добавляет новые city-scoped летописи без ломки модели журнала.
- Для мировых событий потребуется отдельная модель и отдельный UI-поток.


Модель городской экономики описана в `docs/world-sim/13_city_economy_model.md`.
