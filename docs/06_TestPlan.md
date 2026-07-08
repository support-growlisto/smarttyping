# 06 — Test Plan

## 1. Scope

Focus MVP testing on **pure core logic**, where bugs are most costly and testing is cheapest:
keyboard layout conversion, snippet lookup/expansion, and template variable replacement.
Windows API integration (hooks, injection) is validated manually in the MVP and automated later.

## 2. Test levels

| Level        | What                                              | Tooling         |
|--------------|---------------------------------------------------|-----------------|
| Unit         | Converter, template engine, snippet matching, settings logic | xUnit |
| Component    | Application services with in-memory fake ports    | xUnit + fakes   |
| Integration  | Dapper repositories against a temp SQLite file (later) | xUnit (deferred) |
| Manual/E2E   | Hotkey conversion & injection in real apps        | Test checklist  |

## 3. Unit test cases (MVP — automated)

### 3.1 Keyboard layout converter
| ID    | Input           | Direction        | Expected            |
|-------|-----------------|------------------|---------------------|
| KC-1  | `l;ylfu`        | EnToThai         | `สวัสดี`            |
| KC-2  | `สวัสดี`        | ThaiToEn         | `l;ylfu`            |
| KC-3  | round-trip      | EN→TH→EN         | original preserved  |
| KC-4  | `hello 123`     | (unmapped)       | digits/space unchanged |
| KC-5  | auto-detect     | mostly Thai text | direction = ThaiToEn |

### 3.2 Template engine
| ID    | Content              | Expected                         |
|-------|----------------------|----------------------------------|
| TE-1  | `Today is {date}`    | `Today is <short date>`          |
| TE-2  | `Now: {time}`        | `Now: <short time>`              |
| TE-3  | `{clipboard}`        | current clipboard text           |
| TE-4  | `{unknown}`          | `{unknown}` (unchanged)          |
| TE-5  | `{Date}` (mixed case)| replaced (case-insensitive)      |

### 3.3 Snippet expansion
| ID    | Scenario                                   | Expected                              |
|-------|--------------------------------------------|---------------------------------------|
| SE-1  | enabled snippet exists                     | returns rendered content              |
| SE-2  | disabled snippet                           | no expansion                          |
| SE-3  | unknown trigger                            | no match                              |
| SE-4  | trigger case-insensitive (`/Sig` vs `/sig`)| matches                               |
| SE-5  | successful expansion                       | usage count incremented, history added |

## 4. Manual test checklist (hooks/injection)

- [ ] `Ctrl+Shift+L` converts a selection in Notepad, WordPad, and a browser text box.
- [ ] Conversion falls back gracefully when a selection cannot be captured.
- [ ] Injection uses clipboard-paste fallback when direct typing fails.
- [ ] App does not crash if a hook throws; error is logged.
- [ ] Tray toggles enable/disable features live.

## 5. Determinism

- Time-dependent tests inject an `IDateTimeProvider` fake (fixed clock) — no reliance on `DateTime.Now`.
- Clipboard-dependent tests use a fake `IClipboardService`.

## 6. Exit criteria (MVP)

- All automated unit tests pass (`dotnet test` green).
- Manual checklist passes on a Windows 10 and a Windows 11 machine.
- No unhandled exceptions in logs during a 15-minute smoke session.

## 7. Coverage goals

- Converter, template engine, snippet expansion: ~100% of branches.
- Repositories/hooks: not gated for the MVP; add integration tests before v1.0.
