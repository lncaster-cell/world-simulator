# 12 — Future NWN2 Integration

## MVP status

Not implemented in MVP 0.1.

## Constraints to lock now

- Stable city ID: `city_gotha`.
- Simulation core remains independent from NWN2 runtime.
- Future export provides aggregate state outputs only.
- Daily simulation fluctuations must not directly rewrite specific NPC identities/roles.

## Behavioral example

A crisis may affect prices, assortment, dialogue flavor, or service availability, but should not randomly remap NPC existence/status day-to-day.
