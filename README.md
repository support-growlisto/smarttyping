# SmartTyping Desktop

A Windows desktop productivity app that combines **Thai/English language correction**
(RightLang-style) with **text expansion and templates** (Text Blaze-style).

Type a shortcut like `/phone` and it expands to your configured text. Mistype Thai in
the wrong keyboard layout and fix it with a single hotkey. All local, all under your control.

---

## Project overview

SmartTyping Desktop helps bilingual (Thai/English) users type faster and correct
common mistakes without leaving their current application:

- **Snippet expansion** — short triggers expand into longer text, with categories,
  enable/disable, and usage tracking.
- **Language converter** — convert text typed in the wrong keyboard layout between
  Thai Kedmanee and English QWERTY (e.g. `l;ylfu` → `สวัสดี`).
- **Template variables** — insert dynamic values such as `{date}`, `{time}`, and
  `{clipboard}` inside snippets.

The app is designed to be **safe and explicit**: in the MVP nothing is replaced
automatically as you type. Correction and expansion happen on an explicit hotkey or
button press.

---

## MVP scope

**In scope (v0.1):**

1. **Snippet expansion** — `/trigger` → text, stored in SQLite, category support,
   enable/disable, usage count.
2. **Language converter** — Thai Kedmanee ↔ English QWERTY mapping; convert selected
   text or the last typed word via hotkey (`Ctrl + Shift + L`).
3. **Template variables** — `{date}`, `{time}`, `{clipboard}` (user-input placeholders planned, not required).
4. **Settings** — toggle language correction and snippet expansion (startup-with-Windows and hotkey config planned).

**Intentionally NOT in scope (yet):**

- Automatic / as-you-type correction (hotkey/manual only for now).
- AI-assisted correction or suggestions.
- Cloud sync.
- Plugin system.
- Global destructive replacement without a user action.

See [`docs/09_Roadmap.md`](docs/09_Roadmap.md) for what comes after the MVP.

---

## Tech stack

| Concern            | Choice                          |
|--------------------|---------------------------------|
| Runtime            | .NET 9                          |
| Language           | C# (nullable enabled)          |
| UI                 | WPF + MVVM                      |
| Persistence        | SQLite                         |
| Data access        | Dapper                         |
| Logging            | Serilog                        |
| Testing            | xUnit                          |
| Architecture       | Clean Architecture             |

---

## Repository structure

```
smarttyping/
├── README.md
├── CHANGELOG.md
├── Directory.Build.props        # shared MSBuild settings (nullable, langversion, versioning)
├── SmartTyping.sln
├── docs/                        # charter, PRD, SRS, architecture, DB, UI/UX, test plan, ADRs
├── src/
│   ├── SmartTyping.Domain/          # entities, value objects, enums — no dependencies
│   ├── SmartTyping.Application/     # use cases, interfaces (ports), DTOs, app services
│   ├── SmartTyping.Infrastructure/  # SQLite + Dapper, Windows hooks, clipboard, injection, logging
│   ├── SmartTyping.UI/              # WPF, MVVM, views, viewmodels, tray, DI composition root
│   └── SmartTyping.Shared/          # cross-cutting primitives (Result, guards) — no external deps
├── tests/
│   └── SmartTyping.Tests/           # unit tests for core logic
├── assets/                      # icons, images
└── scripts/                     # build / packaging helpers
```

Dependency direction (Clean Architecture):

```
UI ──► Application ──► Domain
Infrastructure ──► Application ──► Domain
Shared ◄── (all layers may reference Shared)
```

Domain depends on nothing. Application depends only on Domain (+ Shared). Infrastructure
and UI depend inward; nothing depends outward on Infrastructure or UI.

---

## Prerequisites

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

Verify the SDK is available:

```powershell
dotnet --version   # expect 9.x
```

---

## How to build

From the repository root:

```powershell
dotnet restore
dotnet build -c Debug
```

Release build:

```powershell
dotnet build -c Release
```

## How to run

```powershell
dotnet run --project src/SmartTyping.UI
```

The app launches a main window (snippet manager) and a system tray icon. On first run the
SQLite database is created automatically under
`%LOCALAPPDATA%\SmartTyping\smarttyping.db` and seeded with default settings and a few
sample snippets.

**Global hotkeys** (explicit action — nothing happens automatically):

- **Ctrl + Shift + L** — convert text between Thai Kedmanee and English QWERTY. With a selection it converts that; with no selection it converts the **last typed word**.
- **Ctrl + Shift + E** — expand the selected trigger (e.g. select `/phone`, press the hotkey).
- **Ctrl + Shift + Space** — open the **quick-picker**: search your snippets and insert one at the caret.

The UI is **Thai by default**; switch to English in Settings.

## How to publish (self-contained single file)

```powershell
dotnet publish src/SmartTyping.UI -c Release /p:PublishProfile=win-x64
# → src/SmartTyping.UI/bin/Release/net9.0-windows/win-x64/publish/SmartTyping.exe
```

## How to test

```powershell
dotnet test
```

---

## Development rules

- **Nullable reference types are on** everywhere. Do not disable them.
- **Async where appropriate** — I/O (DB, clipboard) is async; pure CPU mapping is sync.
- **Dependency injection** — no `new`-ing infrastructure inside application/UI code. Register in the UI composition root.
- **No static business logic** except pure utility/mapping constants (e.g. the keyboard layout tables).
- **Interfaces for infrastructure concerns** — every I/O boundary is a port defined in Application, implemented in Infrastructure.
- **Windows API stays in Infrastructure** — no `user32.dll` P/Invoke in Domain, Application, or UI code-behind logic.
- **Meaningful names**; XML comments only where behavior is non-obvious.
- **Tests for core logic** — converters, snippet matching, template replacement must be covered.
- **Conventional Commits** — `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.
- **Do not commit user data** — `*.db`, `logs/`, and `bin/obj` are git-ignored.

---

## License

[MIT](LICENSE) — a permissive default chosen for the MVP. Change it if your project needs different terms.
