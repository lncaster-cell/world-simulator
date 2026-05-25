# 03 — City Data Model

## City entity (MVP)

Required fields for `city_gotha`:

- identity: `city_id`, `name`;
- demography: `population`;
- economy/state metrics: `wealth`, `mood`, `security`, `crime`;
- stocks: `food`, `resources`, `goods`;
- aggregate status: `city_state`.

## Rules

- `city_id` is stable and immutable for long-term compatibility.
- Daily recalculation updates metrics and stocks.
- `city_state` is derived from aggregate conditions and should avoid day-to-day noise flips.
