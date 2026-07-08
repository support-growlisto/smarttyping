# 07 — Development Guide

## 1. Prerequisites

- Windows 10/11
- .NET 9 SDK (`dotnet --version` → 9.x)
- An IDE: Visual Studio 2022 (17.12+), JetBrains Rider, or VS Code + C# Dev Kit.

## 2. Getting started

```powershell
git clone <repo> smarttyping
cd smarttyping
dotnet restore
dotnet build
dotnet run --project src/SmartTyping.UI
```

Run tests:

```powershell
dotnet test
```

## 3. Solution layout

See [`03_Architecture.md`](03_Architecture.md). Golden rules:

- **Domain** references nothing but Shared. No SQLite, no WPF, no Windows API.
- **Application** defines interfaces (ports) and pure services. It never references Infrastructure.
- **Infrastructure** implements Application ports. All P/Invoke lives here.
- **UI** is the only composition root (DI wiring in `App.xaml.cs`).
- **Shared** holds tiny cross-cutting primitives with no external dependencies.

## 4. Coding standards

- Nullable reference types **on**; fix warnings, don't suppress.
- File-scoped namespaces; `using` outside the namespace.
- `async`/`await` for all I/O (DB, clipboard, injection). Pure mapping stays synchronous.
- Constructor injection everywhere; no service locator, no `new` of infrastructure in UI/App layers.
- No static business logic except pure constant tables (keyboard maps) and pure helpers.
- Meaningful names; XML doc-comments only where behavior is non-obvious.
- Keep methods small; prefer `Result<T>` over throwing across layer boundaries.

## 5. Adding a feature (recipe)

1. Model it in **Domain** (entity/value object/enum) if it's a concept.
2. Define the **port(s)** and **DTOs** in Application; write the service using the ports.
3. Write **unit tests** for the service with in-memory fakes.
4. Implement the port(s) in **Infrastructure**.
5. Wire DI + UI in **UI**.
6. Update `CHANGELOG.md` and relevant docs.

## 6. Working with the database

- Schema changes go into `DatabaseInitializer` behind a version bump (`SchemaVersion`).
- Never hand-edit the shipped DB; treat it as user data.
- Local dev DB lives at `%LOCALAPPDATA%\SmartTyping\smarttyping.db`. Delete it to reset.

## 7. Windows input code

- Hooks/injection are isolated in Infrastructure behind `IKeyboardHook`, `ITextInjector`,
  `IClipboardService`. Do not call `user32.dll` from anywhere else.
- Always log-and-continue on OS faults; never let a hook exception crash the app.

## 8. Commit convention (Conventional Commits)

```
feat: add snippet usage counter
fix: correct Kedmanee mapping for sara-am
docs: expand database schema notes
refactor: extract template token parser
test: add converter round-trip cases
chore: bump Serilog to x.y.z
```

Branch names: `feat/<slug>`, `fix/<slug>`, `docs/<slug>`.

## 9. Definition of Done

- Code compiles with no new warnings; nullable clean.
- Unit tests added/updated and green.
- Docs/CHANGELOG updated.
- Manual smoke test for anything touching hooks/injection.

## 10. Troubleshooting

| Symptom                              | Likely cause / fix                                  |
|--------------------------------------|-----------------------------------------------------|
| Hotkey does nothing                  | Another app owns `Ctrl+Shift+L`; check logs.        |
| Injection inserts nothing            | Target blocks synthetic input; clipboard has result — paste manually. |
| DB errors on startup                 | Corrupt/locked file; close other instances or delete the DB to reset. |
| Thai renders as boxes                | Missing Thai font fallback; verify system Thai support. |
