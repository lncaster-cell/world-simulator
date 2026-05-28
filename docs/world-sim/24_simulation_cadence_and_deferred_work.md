# 24 — Simulation Cadence and Deferred Work

## Purpose

This document records the current direction after the workforce/economy work and the simulation-cadence pivot.

It exists so future AI agents do not lose the previous work or duplicate already merged PRs.

## Already implemented

The following workforce layers already exist in `main`:

- population demographics by race group, children, adult men, adult women, and elderly;
- workforce calculation from demographics and law-profile rates;
- local sector capacity profiles per settlement;
- automatic workforce allocation;
- production calculators using assigned workers for agriculture, fishing, hunting, resource gathering, and crafting;
- read-only city workforce UI tab.

Do not reimplement these.

## Important limitation

The current workforce allocator is still a temporary allocation model.

A city should not freely reassign every worker every day as if a carpenter can become a blacksmith overnight. The current model is useful as a foundation, but it must evolve toward stable sector assignments / occupations.

Future direction:

- introduce persistent workforce sector assignments;
- allow slow reallocation over time;
- separate short-term labor pressure from long-term professions;
- keep emergency temporary labor possible, but limited.

## Population/demographics limitation

Population and demographics need additional work.

Current risk:

- `City.Population` can change through population simulation;
- `City.Demographics.TotalPopulation` must remain synchronized with `City.Population`;
- workforce calculation uses demographics, so a desync would make workforce inaccurate.

Future direction:

- population changes should update demographics;
- births, deaths, aging, migration, and race composition should be modeled in later cadence-based steps;
- demographic aging should not run daily.

## Cadence model

The simulator should use explicit calculation cadences instead of running all systems every day or hiding period checks inside individual steps.

Supported cadences:

- Daily
- Weekly
- Monthly
- HalfYearly
- Yearly

Default calendar policy:

- 7 days per week
- 30 days per month
- 180 days per half-year
- 360 days per year

## Suggested distribution

### Daily

Use for volatile, survival-critical, or arrival-based systems:

- event ticking and event effects;
- workforce allocation preview/current day assignments;
- food production and food consumption;
- resource gathering;
- goods crafting;
- household consumption;
- shipment arrival/return processing.

### Weekly

Use for short-term social/economic adjustments:

- crime flow;
- trade planning;
- caravan hiring;
- guard response to crime/security pressure;
- local logistics balancing.

### Monthly

Use for medium-term settlement state changes:

- population change;
- demographic synchronization after population change;
- infrastructure upkeep/wear;
- maintenance workers effect;
- treasury/tax/expense cycles when those systems exist.

### HalfYearly

Use for slow structural changes:

- demographic aging batches;
- seasonal migration pressure;
- accumulated infrastructure degradation/repair review;
- agricultural season modifiers if needed later.

### Yearly

Use for long-term world history and demographic/economic summaries:

- births/deaths summary if modeled at that layer;
- major prosperity/collapse review;
- city rank upgrade/downgrade;
- law/policy review;
- future clan/state/faction yearly logic.

## Implementation rule

Critical emergency effects may remain daily even if their long-term version is monthly or yearly.

Example: a city with no food should not wait 30 days before suffering any consequences.

## Next recommended PRs

1. Split step order into cadence-specific groups.
2. Move population change to monthly while keeping emergency starvation handling daily if needed.
3. Synchronize demographics with population changes.
4. Replace daily free workforce redistribution with persistent sector assignments.
5. Add guard-worker effects to security/crime.
6. Add maintenance-worker effects to infrastructure wear/upkeep.
7. Add trade-worker effects to trade efficiency.
