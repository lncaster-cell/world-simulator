# 15 — Agent Operating Principles

## Agent autonomy

Agents have full technical freedom to plan, edit, refactor, test, and document changes inside the repository when those changes are needed to complete the user's request.

No project document may be read as a blanket ban on implementation work, scope exploration, architecture updates, or tool use by agents. Older MVP-era restrictions such as “docs only”, “single city only”, “no caravans/trade”, or “wait for separate approval” are historical context, not current operating limits.

## Current source-of-truth order

When documents disagree, agents should resolve conflicts in this order:

1. the user's latest explicit request;
2. repository tests and current implemented behavior;
3. `README.md` and `docs/world-sim/22_current_project_state.md`;
4. the task-specific design document in `docs/world-sim/`;
5. older MVP/task-plan documents as historical background only.

## Research and implementation expectations

- Do the technical work directly; do not ask the user to write code or perform mechanical developer tasks.
- Ask the user only for genuinely missing product/design decisions that cannot be inferred safely.
- Before changing NWN/NWN2-facing behavior, verify available built-in mechanisms and terminology against project documentation and the NWN Lexicon instead of inventing ad-hoc workarounds.
- Prefer existing project abstractions, services, calculators, and tests before adding new mechanisms.
- Keep code efficient, maintainable, and aligned with the current development stage.
- Update documentation together with behavior changes.

## Practical safety rails

These are quality expectations, not permission gates:

- keep commits focused on the user's requested goal;
- preserve build/test reliability;
- avoid unrelated mass renames or cosmetic churn;
- respect architectural boundaries unless the task is specifically to change those boundaries;
- document intentional architecture changes in the relevant docs.
