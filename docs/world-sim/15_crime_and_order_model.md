# 15 — Crime and Order Model (future)

## Purpose

This document describes **future** crime-control mechanics for the city simulation.

`CrimeFlow` and `CrimePressure` already explain:

- where crime growth comes from;
- what damage crime causes.

This document adds the missing part: **how ruler/player and city institutions can fight Crime over time**.

## Core principles

- `Crime` is an aggregated city-level indicator, not a per-criminal simulation.
- `Crime` is never fully removable.
- **Minimum `Crime` is always `1`.**
- Even with very high `Security`, strong institutions, and active interventions, `Crime` cannot become `0`.

### Fixed boundaries

- `Crime` range: `1..100`.
- `Crime = 1` means near-ideal order with residual petty crime.
- `Crime = 0` is invalid in model terms and must never be produced by balancing logic.

## 1) Passive order systems (city infrastructure)

Passive systems are long-term structural institutions that continuously reduce criminal pressure.

### Security level

High `Security` provides baseline suppression of crime growth and improves resilience during shocks.

### Guard

City guard is the primary civil order force:

- routine deterrence;
- rapid response to incidents;
- support for patrol routines.

### Garrison

Garrison supports internal stability under stress:

- reinforces order during unrest;
- raises hard-capacity for crisis containment.

### Patrols

Patrol systems reduce visible criminal activity and improve daily control of districts.

### Court

Court provides lawful resolution and legitimacy:

- reduces unresolved conflict pressure;
- channels punitive action through institutions instead of chaos.

### Prison

Prison provides detention capacity and contributes to long-term pressure reduction.

### Guardhouse

Guardhouse increases local response speed and district-level enforcement readiness.

### Town Hall

Town hall anchors governance coordination:

- policy execution;
- administrative support for order institutions;
- coherent response planning.

## 2) Active ruler/player actions

Active actions are targeted interventions initiated by ruler/player.

### Intensify patrols

Short/mid-term action to increase immediate street control and suppress spikes.

### Fund the guard

Direct allocation of `Wealth` to increase guard effectiveness and sustainability.

### Gang crackdown

Focused anti-criminal operation with stronger immediate effect and higher social/economic cost risk.

### Investigate crisis

Analytical action to diagnose root causes of a crime spike and unlock better follow-up decisions.

### Declare emergency

Hard intervention mode for critical situations. Strong short-term stabilizing potential with notable tradeoffs.

### Invest Wealth into order

Generic budgetary lever to convert `Wealth` into institutional order capacity.

## 3) Tradeoffs and balancing constraints

Crime control always has costs and side effects.

- Fighting crime consumes `Wealth`.
- Hard measures may reduce `Mood`.
- Soft measures are safer for `Mood` but act slower.
- High `Security` lowers `Crime`, but does not eliminate it.
- **Minimum `Crime` remains `1` in all scenarios.**

## 4) Future laws system

Future legal layers can modulate both sources of crime and effectiveness of order responses.

### Soft laws

Lower social pressure but weaker deterrence.

### Strict laws

Higher deterrence and faster suppression, usually with stronger `Mood` risk.

### Unjust laws

May increase hidden tension and long-term crime pressure despite formal strictness.

### Corrupt laws

Reduce institutional effectiveness and can amplify crime persistence.

### Regional mentality

Local culture/mentality modifies law impact and player action outcomes.

## 5) MVP boundaries and integration notes

For MVP and near-term phases:

- no simulation of individual criminals;
- `Crime` remains an aggregate indicator;
- events can create temporary or sustained crime spikes;
- DM/admin/player systems will be able to influence `Crime` through explicit actions.

## Implementation status note

This document is design-level only.

- No gameplay code changes are included here.
- Numeric balancing values, exact formulas, and action cooldowns are future implementation tasks.
