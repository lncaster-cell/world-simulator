# World Simulator (NWN2 External)

This repository contains design documentation and the initial **C#/.NET/WPF technical skeleton** for an external world simulator for an NWN2 module.

## Current stage

- Current stage: **Task 01 skeleton implementation**.
- A minimal Windows desktop app shell exists.
- No simulation systems are implemented yet.

## MVP 0.1 focus

MVP 0.1 remains strictly limited to a single city: **Gotha**.

## Solution layout

```text
WorldSimulator.sln
src/
  WorldSimulator.App/          # WPF UI shell
  WorldSimulator.Core/         # future simulation logic
  WorldSimulator.Persistence/  # save/load JSON layer
```

## Open and run (Windows)

1. Open `WorldSimulator.sln` in Visual Studio 2022 (or later).
2. Set `WorldSimulator.App` as startup project.
3. Build solution:

   ```bash
   dotnet build WorldSimulator.sln
   ```

4. Run app:

   ```bash
   dotnet run --project src/WorldSimulator.App/WorldSimulator.App.csproj
   ```

The app currently opens a placeholder main window only.

## Как получить exe

Обычный CI (**.NET build and test**) больше **не** создаёт `.exe` автоматически на каждый push/PR и выполняет только проверку кода (restore/build/test).

Чтобы собрать Windows `.exe`, используйте ручной workflow:

1. Откройте вкладку **Actions** в репозитории GitHub.
2. В списке workflow выберите **Publish Windows executable**.
3. Нажмите **Run workflow**.
4. Выберите ветку `main`.
5. Подтвердите запуск и дождитесь завершения job.
6. Откройте завершившийся запуск workflow и в блоке **Artifacts** скачайте `world-simulator-win-x64`.
7. Распакуйте архив `.zip`.
8. Запустите `WorldSimulator.App.exe`.

Workflow `Publish Windows executable` публикует self-contained `win-x64` single-file сборку и хранит artifact 14 дней.

## Documentation

All project docs are located in:

- `docs/world-sim/`
