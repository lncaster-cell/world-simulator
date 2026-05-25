# 17 — Application Stack

## Purpose

This document records the recommended technical stack for MVP 0.1.

The user does not need to manually program the stack. The purpose is to give agents/Codex a clear implementation direction.

## Chosen stack for MVP 0.1

```text
Language: C#
Platform: .NET
Desktop UI: WPF
Persistence: local JSON files
Target OS: Windows
```

## Why C#

C# is a practical fit for this project because:

- it is strong for Windows desktop applications;
- it is easier and safer for this kind of tool than C++;
- it works well with structured data and JSON;
- it is suitable for simulation logic, UI, and local files;
- agents/Codex can work with it reliably.

## Why WPF

WPF is chosen for MVP because:

- the project is Windows-only for now;
- it supports normal desktop windows, panels, buttons, tabs, and lists;
- it can display a map image with clickable overlays;
- it is mature and well documented;
- it avoids the heavier setup of Electron or Unity.

## Why JSON saves

JSON is chosen for MVP saves because:

- it is human-readable;
- it is easy to diff in Git;
- it is enough for one-city MVP;
- it keeps persistence simple.

## Alternatives considered

### C++

Rejected for MVP.

Reason: too much complexity for a data/UI simulator. The project does not need low-level performance or engine-level control.

### Avalonia

Possible later alternative.

Reason not chosen now: WPF is simpler for a Windows-only MVP and has more traditional examples.

### Electron / TypeScript

Possible for a web-like UI later.

Reason not chosen now: heavier dependency chain and larger application footprint.

### Python desktop UI

Rejected for MVP.

Reason: easier for quick scripts, weaker for a polished long-term Windows desktop application.

## Non-goals

The MVP stack does not include:

- Unity;
- Unreal;
- C++ engine layer;
- web backend;
- cloud database;
- NWN2 plugin/runtime integration;
- multiplayer server.

## Agent guidance

Agents must not change the stack without a dedicated architecture decision document.

If a future PR proposes Avalonia, Electron, database persistence, or NWN2 integration, it must first update the architecture docs and explain the migration reason.
