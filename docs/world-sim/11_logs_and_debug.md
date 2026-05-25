# 11 — Logs and Debug

## Типы журналов

1. **Летопись города (user-facing, current MVP):** читаемая дневная хронология состояния конкретного города.
2. **Технический журнал (debug):** подробная раскладка расчётов.
3. **Летопись мира (future feature):** отдельный журнал крупных глобальных событий (не реализован в MVP).

## City-scoped журнал в текущем MVP

- Текущий пользовательский журнал — это **летопись выбранного города**, а не общий журнал мира.
- Каждая запись хранит `CityId` и `CityName`.
- В текущем MVP используется один город: `CityId = "gotha"`, `CityName = "Гота"`.
- Фильтры (`Все`, `События`, `Население`, `Пища`, `Состояние`, `Система`, `Ошибки`, `Карта/отладка`) применяются **внутри выбранного города**.
- Нельзя смешивать записи разных городов в один пользовательский журнал: при расширении до multi-city должна сохраняться модель "один город — одна летопись".

## Летопись мира (future feature, not in this PR)

Будущая летопись мира должна хранить только крупные глобальные события, например:

- падение города;
- начало войны;
- смена владельца региона;
- крупный кризис.

В этом этапе **не** реализуются:

- UI летописи мира;
- агрегация всех городов в один пользовательский журнал;
- выбор города внутри окна летописи.

## Пример технического журнала

Day 5:

- Food: 1000 → 910
- population consumption: -84
- fishing: TBD
- hunting: TBD
- mainland supplies: TBD
- event “rats”: TBD
- total: -90

## Debug panel (required)

Must allow:

- add/remove food;
- add/remove wealth;
- add/remove resources;
- add/remove goods;
- set mood;
- set security;
- set crime;
- trigger event manually;
- skip hour;
- skip day;
- save;
- load;
- reset Gotha to start preset.

## Retention

- Technical log shows only the latest 500 entries.
- User city chronicle keeps only the latest 500 daily entries in runtime memory.
- Journals are runtime-only and are not persisted to save files.
