# World Simulator (NWN2 External)

Репозиторий содержит **рабочий C#/.NET/WPF симулятор мира** для NWN2-модуля (как внешнее desktop-приложение), а также проектную документацию.

## Текущее состояние на 26 May 2026

> Важно: часть старых документов в `docs/world-sim/` отражает ранний «docs-only» этап и исторически не соответствует текущей реализации.

Уже реализовано:
- доменная модель города и пресеты (включая Gotha);
- симуляционные калькуляторы: еда, богатство, преступность, потребление, производство;
- события города, генератор и применение эффектов;
- караваны/торговые сущности;
- simulation clock;
- JSON save/load;
- WPF UI (карта, окно города, журнал).

## Быстрый навигатор для ИИ-агентов

- **Стартовая точка для восстановления контекста:** `docs/world-sim/21_agent_navigation_map.md`
- **Операционные принципы для агента:** `docs/world-sim/15_agent_instructions.md`
- **Архитектура по слоям:** `src/WorldSimulator.App`, `src/WorldSimulator.Core`, `src/WorldSimulator.Persistence`
- **Проверка изменений по логике:** `tests/WorldSimulator.Core.Tests`
- **Проверка save/load:** `tests/WorldSimulator.Persistence.Tests`

Рекомендуемый порядок чтения для нового агента:
1. `README.md`
2. `docs/world-sim/21_agent_navigation_map.md`
3. Нужный раздел в `docs/world-sim/` по задаче
4. Соответствующие тесты (как источник фактического поведения)

## Как использовать загруженную карту маршрутов (`data/regions/rivia/routes/v1/`)

Канонический граф маршрутов лежит в `data/regions/rivia/routes/v1/`: `route_nodes.csv`, `route_edges.csv`, `route_todo.csv`, `rivia_routes.json`.

Сейчас симулятор читает маршруты из кодовых пресетов `src/WorldSimulator.Core/Trade/TradeRoutePresets.cs`, поэтому рабочий поток такой:
1. Считать `route_edges.csv` как source of truth по `travel_days`.
2. Обновить `TravelDays` и `DistanceDays` в `TradeRoutePresets` под канонические значения.
3. Для новых направлений добавить `TradeRoute` с `FromSettlementId/ToSettlementId` и `Points` в нормализованных координатах карты (0..1).
4. Неподтверждённые линии из `route_todo.csv` не включать в `IsEnabled = true`, пока не зафиксирована длительность.
5. После изменений обязательно прогнать `WorldSimulator.Core.Tests`, особенно тесты на торговые маршруты/караваны.

Это позволяет использовать новую карту уже сейчас без изменения формата сохранений или UI-контрактов.

## Структура решения

```text
WorldSimulator.sln
src/
  WorldSimulator.App/          # WPF UI
  WorldSimulator.Core/         # симуляция и доменная логика
  WorldSimulator.Persistence/  # JSON save/load
tests/
  WorldSimulator.Core.Tests/
  WorldSimulator.Persistence.Tests/
docs/
  world-sim/
```

## Сборка/запуск (Windows)

1. Открыть `WorldSimulator.sln` в Visual Studio 2022+.
2. Назначить `WorldSimulator.App` startup project.
3. Сборка:
   ```bash
   dotnet build WorldSimulator.sln
   ```
4. Запуск:
   ```bash
   dotnet run --project src/WorldSimulator.App/WorldSimulator.App.csproj
   ```

## Как получить `.exe`

CI ` .NET build and test` выполняет валидацию (restore/build/test), но не публикует `.exe` на каждый push/PR.

Для публикации использовать ручной workflow **Publish Windows executable**:
1. GitHub → **Actions**.
2. Выбрать workflow **Publish Windows executable**.
3. Нажать **Run workflow**.
4. Выбрать ветку `main`.
5. После завершения скачать artifact `world-simulator-win-x64`.
6. Распаковать `.zip` и запустить `WorldSimulator.App.exe`.

## Диагностика

Если приложение закрывается при старте/не открывается, проверить `logs/startup-crash.log` рядом с `.exe`.
