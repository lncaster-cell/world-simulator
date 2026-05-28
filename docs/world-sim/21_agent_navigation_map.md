# 21 — Agent Navigation Map (Context Recovery Guide)

## Purpose

Этот документ — «карта проекта» для ИИ-агентов (в т.ч. ChatGPT Codex), чтобы быстро:
- восстановить контекст между сессиями;
- понять, где находится нужная логика;
- безопасно выполнять рефакторинг без потери целостности.

## 1) Session Re-entry Protocol (быстрый вход в контекст)

При старте новой сессии:
1. Прочитать `README.md`.
2. Прочитать этот документ (`21_agent_navigation_map.md`).
3. Определить целевой слой изменений: `App` / `Core` / `Persistence` / `Tests` / `Docs`.
4. Прочитать соответствующие тесты до кода реализации.
5. Проверить, нет ли конфликтов со старыми docs, созданными на раннем docs-only этапе.

## 2) Source of Truth by Layer

### UI Layer (`src/WorldSimulator.App`)
- `MainWindow.xaml(.cs)` — главный экран/карта.
- `CityWindow.xaml(.cs)` — окно города.
- `LogWindow.xaml(.cs)` — журнал/тех-логи.
- `ViewModels/MainWindowViewModel.cs` — orchestration между UI и Core.

Когда менять:
- только UI-поведение, формат отображения, команды, привязки.

### Simulation Core (`src/WorldSimulator.Core`)
- `Cities/` — модель города, состояния, эволюция состояния.
- `Resources/` — production/consumption/wealth/crime calculators.
- `Events/` — городские события, генерация, эффекты, менеджмент.
- `Trade/` — модель караванов.
- `Time/` — simulation clock.
- `World/` — регионы, пресеты мира, локации.

Когда менять:
- бизнес-логику, расчёты, правила переходов состояний, симуляционные эффекты.

### Persistence (`src/WorldSimulator.Persistence`)
- `Saves/WorldSaveData.cs` — контракт сохранения.
- `Saves/JsonWorldSaveService.cs` — чтение/запись JSON.

Когда менять:
- схему сохранений или сериализацию.

### Tests (`tests/`)
- `WorldSimulator.Core.Tests` — поведение доменной логики.
- `WorldSimulator.Persistence.Tests` — корректность save/load.

Принцип:
- перед изменением логики найти релевантный тест;
- после изменения обновить/добавить тест на новое поведение.

## 3) Feature-to-File Index (что где искать)

- Симуляционное время: `Core/Time/*`
- Производство и потребление: `Core/Resources/*`
- События города: `Core/Events/*`
- Состояние города: `Core/Cities/CityStateEvaluator.cs`
- Пресеты города/мира: `Core/Cities/CityPresets.cs`, `Core/World/*Presets.cs`
- UI-композиция: `App/MainWindow*`, `App/CityWindow*`, `App/ViewModels/*`
- Сохранения: `Persistence/Saves/*`

## 4) Refactoring Safety Checklist

Перед рефакторингом:
- зафиксировать входные/выходные контракты затрагиваемых классов;
- проверить существующие тесты в соответствующем модуле;
- исключить cross-layer смешивание (UI logic не уходит в Core и наоборот).

После рефакторинга:
- убедиться, что тесты зелёные;
- обновить docs, если изменились точки входа/архитектурные границы;
- проверить, что сериализация совместима (если затронут save model).

## 5) Repository Conventions for AI Agents

- Agents have full technical freedom to complete the user's requested goal; old MVP-era prohibitions are historical context, not active bans.
- Минимальные PR: одна логическая цель на один PR.
- Документация обновляется вместе с кодом, если поведение изменилось.
- Не переименовывать массово файлы без необходимости.
- Не смешивать «механику» и «косметику» в одном коммите.
- Для NWN/NWN2-facing изменений сверяться с проектной документацией и NWN Lexicon, затем использовать встроенные или уже существующие механики там, где это возможно.

## 6) Legacy Documentation Note

Некоторые документы в `docs/world-sim/` содержат формулировки про раннюю стадию («docs only», «нет реализации»). Их считать историческим контекстом, а не текущим состоянием. Текущее состояние проекта фиксируется в `README.md` и тестах.

## 7) Recommended Task Routing (для Codex)

- UI issue → сначала `App/ViewModels`, затем XAML/code-behind.
- Simulation mismatch → сначала тест, затем `Core` калькуляторы/модели.
- Save/load bug → `Persistence/Saves` + persistence tests.
- Regression risk → добавить/обновить тест до изменений в логике.
