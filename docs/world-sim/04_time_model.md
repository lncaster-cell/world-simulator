# 04 — Time Model

## Controls

- `Start`
- `Pause`

## Pace

- Baseline speed: 5 real minutes = 1 in-game hour.
- 24 in-game hours = 1 in-game day.

## Scheduling design rule (MVP)

The simulator must not wait for an hourly/daily boundary and then compute everything in one heavy burst.

Instead, it must use a lightweight, CPU-friendly runtime loop that:

- continuously tracks elapsed real time;
- checks active timers;
- incrementally prepares pending balances/changes for upcoming logical ticks.

## Tick structure and mutation boundary

- Hourly tick: minor logical tick.
- Daily tick: main logical tick for economy, food, production, and city state recalculation.
- Actual city-state mutation is allowed only on fixed logical ticks (hourly/daily), even if preparatory calculations are updated continuously.

## Performance constraint

- Do not implement a busy loop.
- Runtime scheduling must remain lightweight and CPU-friendly.
