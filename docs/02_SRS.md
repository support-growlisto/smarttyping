# 02 — Software Requirements Specification (SRS)

## 1. Purpose

Detailed, testable requirements for the SmartTyping Desktop MVP. Complements the PRD
with interface-level and behavioral detail.

## 2. System context

A single-user desktop application. Inputs: keyboard events, clipboard, local SQLite DB.
Outputs: text injected into the foreground application, UI, local logs.

```
[User keyboard] ─► [Keyboard hook] ─► [Application services] ─► [Text injection] ─► [Foreground app]
                                            │
                                       [SQLite DB] (snippets, settings, usage)
```

## 3. External interfaces

### 3.1 User interfaces
- Main window: snippet list (filterable by category, searchable), toolbar (add/edit/delete/enable).
- Add/Edit snippet dialog: trigger, content (multiline), category, enabled.
- Settings view: toggles for expansion and language correction.
- System tray icon: show/hide window, toggle features, exit.

### 3.2 Hardware/OS interfaces
- Low-level keyboard hook (`WH_KEYBOARD_LL`) via Infrastructure only.
- Global hotkey registration (`RegisterHotKey`) for `Ctrl+Shift+L`.
- Clipboard read/write; simulated keystrokes / paste for injection.

### 3.3 Software interfaces
- SQLite via Dapper.
- Serilog file sink under `%LOCALAPPDATA%\SmartTyping\logs`.

## 4. Functional requirements (detailed)

### 4.1 Snippet engine
- SRS-1 The system SHALL store snippets with fields: Id, Trigger, Content, CategoryId (nullable),
  IsEnabled, UsageCount, CreatedUtc, UpdatedUtc.
- SRS-2 Triggers SHALL be unique (case-insensitive) and non-empty.
- SRS-3 `SnippetExpansionService.TryExpand(trigger)` SHALL return the resolved content
  (template variables applied) for an enabled snippet, or a no-match result otherwise.
- SRS-4 On a successful expansion the service SHALL increment UsageCount and append a UsageHistory record.

### 4.2 Template engine
- SRS-5 `ITemplateEngine.Render(content)` SHALL replace `{date}`, `{time}`, `{clipboard}`
  and leave unknown tokens unchanged.
- SRS-6 Token matching SHALL be case-insensitive (`{Date}` == `{date}`).

### 4.3 Layout converter
- SRS-7 `IKeyboardLayoutConverter.Convert(text, direction)` SHALL map each character using the
  Kedmanee↔QWERTY table; unmapped characters pass through.
- SRS-8 `DetectDirection(text)` SHALL return the likely conversion direction based on the ratio of
  Thai vs. Latin characters.
- SRS-9 Conversion SHALL be a pure function (no I/O, no side effects) and fully unit-tested.

### 4.4 Settings
- SRS-10 Settings SHALL be key/value rows (`AppSetting`) loaded at startup and cached.
- SRS-11 Changing a toggle SHALL persist immediately.

### 4.5 Input & injection (Infrastructure)
- SRS-12 The keyboard hook SHALL be abstracted behind `IKeyboardHook` (start/stop, key events).
- SRS-13 Text injection SHALL be abstracted behind `ITextInjector` with a clipboard-paste fallback.
- SRS-14 The hotkey handler SHALL run conversion off the UI thread and marshal UI updates back.

## 5. Non-functional requirements

- SRS-15 Nullable reference types enabled; no suppressed nullability warnings in core layers.
- SRS-16 No Windows API calls outside Infrastructure.
- SRS-17 The app SHALL start even if the DB is missing (it self-initializes).
- SRS-18 All exceptions in hooks/injection SHALL be logged and SHALL NOT crash the app.

## 6. Data requirements

See [`04_Database.md`](04_Database.md) for schema. User data is never transmitted off-device.

## 7. Constraints & assumptions

- Single foreground user session; no concurrent multi-instance support in MVP.
- The user has permission to run low-level hooks (standard user is sufficient on Windows 10/11).
- Some applications (elevated/secure desktops, password fields) may block injection — the app degrades gracefully.

## 8. Traceability (PRD → SRS)

| PRD | SRS            |
|-----|----------------|
| FR-1..FR-5 | SRS-1..SRS-4 |
| FR-6..FR-9 | SRS-7..SRS-9 |
| FR-10..FR-11 | SRS-5..SRS-6 |
| FR-12..FR-13 | SRS-10..SRS-11 |
