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

После успешного CI можно скачать готовый Windows `.exe` из GitHub Actions:

1. Откройте вкладку **Actions** в репозитории GitHub.
2. Выберите последний успешный запуск workflow **.NET build and test**.
3. В блоке **Artifacts** скачайте архив `world-simulator-win-x64`.
4. Распакуйте архив `.zip`.
5. Запустите `WorldSimulator.App.exe`.

Локально собрать такой же publish-вывод можно командой:

```bash
dotnet publish src/WorldSimulator.App/WorldSimulator.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

## Documentation

All project docs are located in:

- `docs/world-sim/`
