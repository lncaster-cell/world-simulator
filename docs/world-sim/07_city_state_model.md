# 07 — City State Model

## Definition

`city_state` is a stable aggregated summary of city condition. It should not change from small daily noise.

## MVP state list

- `stable`
- `prosperous`
- `stagnation`
- `food_shortage`
- `famine`
- `economic_decline`
- `crime_problem`
- `unrest`
- `recovery`
- `collapse`

## UI rule

Show factual current state only.

- Valid: “Гота — состояние: стагнация”
- Not allowed: risk/forecast style messaging in UI.
