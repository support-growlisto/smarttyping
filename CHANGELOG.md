# Changelog

All notable changes to SmartTyping Desktop are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial repository skeleton following Clean Architecture (Domain, Application, Infrastructure, UI, Shared).
- Project documentation set under `docs/` (Charter, PRD, SRS, Architecture, Database, UI/UX, Test Plan, Development Guide, Release Plan, Roadmap) and four ADRs.
- Domain entities: `Snippet`, `Category`, `UsageHistory`, `AppSetting`.
- SQLite schema initialization and Dapper repositories.
- Snippet expansion service with template variable replacement (`{date}`, `{time}`, `{clipboard}`).
- Thai (Kedmanee) ↔ English (QWERTY) keyboard layout converter.
- WPF MVVM shell: main window, snippet list, add/edit form, settings placeholder, system tray entry.
- Keyboard hook abstraction and global hotkeys: **Ctrl+Shift+L** (language conversion) and **Ctrl+Shift+E** (snippet expansion of the selected trigger), both wired end-to-end through selection capture → transform → injection.
- System-tray app icon (`assets/app.ico`) and a self-contained single-file publish profile (`win-x64.pubxml`).
- Unit tests for layout conversion, snippet lookup, and template variable replacement (21).
- Integration tests exercising the Dapper repositories against a temporary SQLite file (7).

### Security & reliability
- **Secure-field guard**: conversion/expansion hotkeys now skip when the focused control is a detected native password field (`ES_PASSWORD`), so a password is never copied to the clipboard or overwritten. Best-effort — non-native inputs (browsers/Electron/UWP) can't be inspected; see ADR-002.
- **Full clipboard preservation**: capturing a selection and injecting text now snapshot and restore the *entire* clipboard (all formats), so images/files/rich text you had copied are no longer destroyed. Previously only text was saved/restored.
- **Global crash handling**: `DispatcherUnhandledException`, `AppDomain.UnhandledException`, and `TaskScheduler.UnobservedTaskException` are logged via Serilog; recoverable UI faults no longer crash the process.
- **Single instance**: a named mutex prevents a second launch from installing a duplicate keyboard hook / tray icon / DB writer; the second launch surfaces the running window and exits.

### Robustness & polish
- **Reliable clipboard timing**: injection and selection capture now wait for the clipboard to actually reach the expected state (bounded polling) instead of fixed sleeps, improving reliability on slow/remote apps. Selection capture uses a distinctive sentinel to distinguish "empty selection" from "copy did nothing".
- **UTC `DateTime` mapping**: a Dapper type handler now materializes stored timestamps with `DateTimeKind.Utc` (they were `Unspecified`), keeping the `...Utc` entity fields honest.
- **Release builds embed debug symbols** (`DebugType=embedded`) — the single-file publish output no longer ships loose `.pdb` files.
- **Main window title-bar icon** set from the app icon.
- Added `LICENSE` (MIT) and a GitHub Actions **CI** workflow (build + test on `windows-latest`).
- Test suite grown to **46** (39 unit + 7 integration): added `SettingsService` boolean-parsing tests and `Trigger` value-object tests.

### Fixed
- UTC timestamps now round-trip cleanly through SQLite. Previously storing an ISO-8601 string with a trailing `Z` caused the reader to convert values to local time on read (changing `DateTime.Kind`); timestamps are now stored without a zone designator via a single `SqliteTime` helper, and read back as UTC via a Dapper type handler. Caught by the repository integration tests.

### Snippet toolkit
- **Import / Export snippets as JSON** — portable backup and sharing. Missing categories are recreated on import; trigger conflicts are resolved by choosing **overwrite** or **keep existing (skip)**.
- **Preview / test-expand** button in the add/edit dialog — renders the snippet (template variables applied) inline, with a ▮ marker showing the `{cursor}` position, without switching apps.
- **Richer template variables**:
  - `{date:FORMAT}` / `{time:FORMAT}` — custom .NET formats (e.g. `{date:yyyy-MM-dd}`). Explicit formats use the invariant (Gregorian) calendar, so `{date:yyyy}` is `2026`, not the Buddhist-era `2569`, on Thai systems. Bare `{date}`/`{time}` stay locale-aware.
  - `{date+N}` / `{date-N}` — day offsets (e.g. `{date+7:yyyy-MM-dd}` = one week out).
  - `{cursor}` — marks where the caret lands after insertion (the injector walks the caret back to it).
- Test suite grown to **60** (53 unit + 7 integration), covering the new formats/offsets/cursor and the import/export service (add / skip / overwrite / round-trip / category creation).

### New features
- **Category management**: add / rename / delete categories from a manager window (reached via "Manage" next to the category list). Deleting a category keeps its snippets (they become uncategorized). Duplicate names are rejected.
- **Usage statistics**: a Stats window shows total snippets (and how many enabled), total expansions, estimated time saved (typing avoided), and the most-used snippets — with a "Clear stats" action. Computation lives in a pure, unit-tested `StatsCalculator`.
- **Configurable hotkeys**: all four global hotkeys (convert / expand / quick-picker / add-from-selection) can be rebound from Settings. A recorder captures the combination; duplicates are rejected and a combo must include Ctrl/Alt/Win. Bindings persist and apply live. The `Hotkey` value object (modifiers + key, parse/format) is unit-tested; the low-level hook now matches configurable bindings.
- **Smarter fuzzy search**: the quick-picker now uses subsequence matching (fzf-style) with scoring — prefix, word-boundary, camelCase, and contiguous-run bonuses; trigger matches outrank content matches, ties break by usage. e.g. `phn` finds `/phone`.
- **Dark mode**: light/dark theme with a **follow-system** default, switchable live from Settings (persisted). All windows re-theme instantly via a small `ThemeManager` swapping brush dictionaries; styles reference them with `DynamicResource`.
- **Add snippet from selection** (Ctrl+Shift+N): grabs the selected text and opens the Add dialog pre-filled with it. Skips detected password fields.
- **`{input:Label}` template placeholders**: snippets can prompt for values on expansion (e.g. `Dear {input:Name},`). A small form collects one value per unique label; the same label is asked once. Cancelling aborts the expansion and does not count usage. Works for expansion, quick-picker, and the edit-dialog Preview.
- **Convert the last typed word without selecting** (PRD FR-7): if you press Ctrl+Shift+L with no selection, SmartTyping auto-selects the previous word (Ctrl+Shift+Left) and converts it in place.
- **Quick-picker** (Ctrl+Shift+Space): a floating, searchable list of your snippets. Type to fuzzy-filter (trigger-prefix ranks first, then trigger-, then content-matches; ties break by usage), ↑↓ to navigate, Enter to insert at the caret, Esc to dismiss. Focus returns to the app you were typing in, and it won't insert into a detected password field. Ranking lives in `SnippetSearch` (Application) and is unit-tested.

### Distribution & updates
- **Windows installer (Inno Setup)** — `packaging/installer/SmartTyping.iss` + `scripts/build-installer.ps1` build a per-user `SmartTyping-Setup-<version>.exe` with Start Menu / optional Desktop shortcuts and an uninstaller (no admin, no signing cert needed).
- **In-app update check** — opt-in (off by default; the app's only network feature). Checks the GitHub "latest release" endpoint on startup and via Settings → "Check now"; if a newer version exists it offers to open the download page. Version comparison is a pure, unit-tested `UpdateComparer`.

### UI polish
- A refreshed visual design: refined light/dark palettes, an **accent primary button** for the main actions (Add / Save / Insert), rounded text boxes with an accent focus ring, accent-filled check boxes, softer rounded list selection, and a cleaner grid (no gridlines, roomier rows, subtle headers). All themed, so it looks consistent in light and dark.

### Localization
- **Thai / English UI** with **Thai as the default**. Switch language live from Settings (no restart); the choice is remembered. A lightweight `LocalizationManager` drives all window text, the tray menu, and status/dialog messages via an indexer binding. Only UI strings are localized — date/number formatting still follows the system culture.

### Ship-readiness
- **First-run onboarding** — a welcome dialog explains the two hotkeys (Ctrl+Shift+L convert, Ctrl+Shift+E expand) and the tray behaviour; shown once, then remembered. The snippet grid now shows an empty-state hint when there are no snippets.
- **Start with Windows** (opt-in) — a Settings toggle registers/unregisters the app in the per-user Run key (no elevation). Off by default.
- **Open logs** — tray menu and Settings button open the log folder for support/diagnostics.
- **winget packaging** — `packaging/winget/` contains portable manifests + a release guide so the app can be published for `winget install SmartTyping.SmartTyping` (fill the release URL/hash before submitting).

### AI & as-you-type suggestions (opt-in, off by default)
- **As-you-type layout correction** — when enabled in Settings, SmartTyping watches the word you're typing and, if it looks like wrong-layout Thai text (e.g. `l;ylfu`), either:
  - **Suggests** the fix via a non-destructive tray hint (default) — nothing is rewritten; you press the convert hotkey to apply; or
  - **Fixes it automatically** (opt-in sub-option): on the next space the word is replaced in place. Automatic mode uses a **stricter rule that ignores the apostrophe**, so English contractions (`don't`, `it's`, `I'm`) are never touched, and it only fires on a space boundary (never across a line break).
  The detection is a pure, unit-tested heuristic (`WrongLayoutDetector`, with a `strict` mode); the hook only tracks plain typing (no modifier keys) and skips when the active layout is already Thai. Suggestions are throttled to at most one hint every few seconds. Off unless you turn it on.

### Automatic snippet expansion (opt-in)
- **Type-to-expand** — turn on "Expand automatically as I type" in Settings and typing a trigger (e.g. `/sig`) followed by a space or tab replaces it in place with the rendered snippet — no hotkey. Skips detected password fields; only single-character delimiters (space/tab) trigger it. The manual select-and-`Ctrl+Shift+E` flow and the quick-picker still work as before.

### Fixed & polished
- **All synthetic input was silently failing** — the Win32 `INPUT` P/Invoke struct's union was missing `MOUSEINPUT`, so `Marshal.SizeOf<INPUT>` was 32 bytes on x64 instead of 40; `SendInput` rejects a wrong `cbSize` and injects nothing. This broke every feature that types for you (layout conversion, snippet expansion, AI-improve, automatic correction) — they appeared to run (and showed a tray balloon) but no text was inserted. Fixed the struct and added a size-regression test.
- **Dark mode was broken in two ways** — (1) the dark palette contained an invalid 10-digit colour (`#FF35356088`), so the whole theme dictionary failed to parse and switching to dark left the UI half-styled; fixed the colour and hardened `ThemeManager` to load the new palette before dropping the old one. (2) Secondary windows (Settings, Stats, etc.) kept a white background in dark mode because an implicit `Style TargetType="Window"` doesn't apply to derived window classes; each window now sets its themed background/foreground explicitly.
- **Per-user installer** — the Inno Setup installer targeted `C:\Program Files` (admin-only) and failed with "Access is denied" for a non-admin install; it now installs under `%LOCALAPPDATA%\Programs\SmartTyping`.
- **Version in the title bar** — the main window title now shows the running version (e.g. "SmartTyping 0.1.2") so the installed build is unambiguous.
- **Blank tray icon / missing notifications** — the app icon wasn't copied next to a single-file publish, so the tray showed a blank icon; a `NotifyIcon` with an invalid icon also suppresses balloon tips, which is why conversion/suggestion notifications never appeared. The icon is now embedded in the assembly and loaded at the exact tray size; balloons use an info glyph and a longer timeout.
- **UI refresh** — a card-based layout with an app header (logo + tagline), an elevated sidebar with accent-bar navigation and grouped ghost actions, a search field with a leading glyph, and monospace "chip" triggers in the snippet grid.
- **AI improve selection** (Ctrl+Shift+I, rebindable) — select text and press the hotkey to have an AI clean up spelling/grammar while keeping the same language and meaning, replacing the selection in place. Provider-agnostic behind an `IAiService` port; ships with a **Google Gemini** (free-tier) implementation. **Bring your own API key** — paste it in Settings; with no key the feature is inert. Text is sent to the provider **only** when you invoke it, never automatically, and password fields are skipped. Failures are logged and swallowed (no replacement on error).

### Notes
- Cloud sync and the plugin system are intentionally **not** implemented in the MVP. See `docs/09_Roadmap.md`.
- `{input:Label}` (interactive placeholder prompts) is scaffolded conceptually but deferred — unknown tokens are preserved verbatim, so it's a non-breaking addition later.

[Unreleased]: https://example.com/smarttyping/tree/main
