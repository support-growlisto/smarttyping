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

## [0.2.0]

### Layout correction is now decided by dictionary, not by character shapes
The hand-written heuristics are gone. A word is converted only when it is a real word in the target
language **and not** a real word in the language it was typed in — the same principle RightLang uses.
Each direction carries its own veto, and that is what makes it safe:

- **Latin layout** → convert only if the Thai reading is a Thai word *and* the latin isn't English.
  `soy'lnv` → `หนังสือ`; `world` and `don't` are untouched.
- **Thai layout** → convert back only if the latin is an English word *and* the Thai on screen isn't a
  Thai word. `hello` is restored; `ทดสอบ` is never touched.

This fixes two classes of bug the heuristic could not: the apostrophe is `ง`, so every Thai word
containing it (`หนังสือ`, `ของ`, `อย่าง`) used to be ignored; and English whose Thai rendering happened
to look structurally valid (`world` → `ไนพสก`) used to be mis-converted.

Both word lists are **public domain** and are not taken from any other keyboard tool:
Thai from [PyThaiNLP](https://github.com/PyThaiNLP/pythainlp) (CC0-1.0, 60,537 words) and English from
[dwyl/english-words](https://github.com/dwyl/english-words) (Unlicense, 315,100 words). They are
gzipped into the assembly (1.2 MB) and loaded off-thread; until they are ready the corrector does
nothing. See `assets/dict/README.md`.

### Fixed: the Thai layout silently drops keystrokes
Windows' Thai layout rejects impossible sequences, so typing `hello` on it inserts only `สสน` — the
leading tone mark and sara-am never reach the document. The corrector used to assume one character per
keystroke and would have backspaced over five characters to fix three, destroying text before the
word. `ThaiInput.Filter` now models that validation, so we delete exactly what is on screen.

### Notifications
- **Tray notifications can be switched off** — Settings → "Show notifications". Gated inside `TrayIconService` rather than at each call site, so no feature can pop a balloon the user disabled. The features themselves keep working silently.

### Layout correction now works both ways
- **Thai layout, English intended** — typing `hello` on the Thai layout produces `้ำสสน`; it is now corrected back to `hello` and the keyboard is switched to English. Detection is structural: Thai that cannot exist (a word beginning with a tone mark or attached vowel, a tone on a tone, a vowel on a vowel) means the user meant English. Deliberately conservative, so correctly-typed Thai (`น้ำ`, `ที่`, `ทดสอบ`, `ไทย`) is never touched — the trade-off is that English whose Thai rendering happens to be structurally valid (`world` → `ไนพสก`) isn't auto-corrected; `Ctrl+Shift+L` still converts it.
- `LayoutCorrection` now carries the direction, and `IKeyboardLayoutSwitcher.SwitchForeground(toThai)` switches either way.

### Automatic, no-hotkey typing (opt-in)
- **Type-to-expand** — turn on "Expand automatically as I type" and a trigger expands **the moment you finish typing it** (e.g. `/sig`), with no hotkey and no space. A trigger that is a prefix of a longer one (`/s` vs `/sig`) instead waits for a space/tab, so you can always type the longer one; that rule lives in a pure, unit-tested `TriggerIndex`. Skips detected password fields.
- **Correct-as-you-type layout fixes** — with "Fix automatically" on, wrong-layout Thai (e.g. `l;ylfu`) is corrected **as soon as it's recognisable, without pressing space**, and the keyboard is switched to Thai so the rest of the word types correctly. That final step matters: correcting the characters typed so far is useless if the next ones keep coming out latin.

### Correctness of synthetic input (found by end-to-end testing, not unit tests)
- **The app corrected and expanded its own output.** Keystrokes we synthesize were re-entering our own low-level hook, so an expansion's text got treated as freshly typed input and run through the layout corrector. Every synthesized event now carries a `SelfInjectedTag` in `dwExtraInfo`, and the hook ignores its own input.
- **Inline replacement no longer uses the clipboard.** It pasted via the clipboard injector, which restores the previous clipboard ~80 ms after Ctrl+V — fired straight from the hook, the target read the clipboard *after* the restore and pasted nothing, so text was deleted but never replaced. Replacements are now typed directly as Unicode keystrokes, which is deterministic and leaves the user's clipboard untouched.
- **Replacements are atomic.** The backspaces, the replacement text, and any caret-restoring Left taps go out in a single `SendInput` call, so a keystroke from a fast typist can't land in the middle of an edit. A short delay first lets the triggering keystroke reach the target window (the hook fires before `CallNextHookEx` delivers it).
- **Quick-picker crash** — accepting or dismissing it called `Close()` while the window was already closing (`InvalidOperationException` from `WmActivate`). Dismissal is now idempotent.
- **ComboBox was unreadable in dark mode** — the stock template paints system chrome; it now has a fully themed template.

### Fixed & polished
- **All synthetic input was silently failing** — the Win32 `INPUT` P/Invoke struct's union was missing `MOUSEINPUT`, so `Marshal.SizeOf<INPUT>` was 32 bytes on x64 instead of 40; `SendInput` rejects a wrong `cbSize` and injects nothing. This broke every feature that types for you (layout conversion, snippet expansion, AI-improve, automatic correction) — they appeared to run (and showed a tray balloon) but no text was inserted. Fixed the struct and added a size-regression test.
- **Dark mode was broken in two ways** — (1) the dark palette contained an invalid 10-digit colour (`#FF35356088`), so the whole theme dictionary failed to parse and switching to dark left the UI half-styled; fixed the colour and hardened `ThemeManager` to load the new palette before dropping the old one. (2) Secondary windows (Settings, Stats, etc.) kept a white background in dark mode because an implicit `Style TargetType="Window"` doesn't apply to derived window classes; each window now sets its themed background/foreground explicitly.
- **Per-user installer** — the Inno Setup installer targeted `C:\Program Files` (admin-only) and failed with "Access is denied" for a non-admin install; it now installs under `%LOCALAPPDATA%\Programs\SmartTyping`.
- **Version in the title bar** — the main window title now shows the running version (e.g. "SmartTyping 0.1.2") so the installed build is unambiguous.

### Hardening (from an internal review pass)
- **No more wrong-place edits** — the as-you-type word buffer is now cleared on mouse clicks (new low-level mouse hook) and on focus changes, so automatic expansion/correction can never backspace-and-replace at a caret position it didn't actually track (a data-loss risk with the auto features on).
- **Shift is honoured** — the word buffer now records the shifted character (uppercase / symbols), so triggers containing them auto-expand and layout conversion of shifted keys is correct.
- **Injection is serialized** — the clipboard save→paste→restore runs under a lock and the auto-correct handler has a re-entrancy guard, so two overlapping injections can't clobber the clipboard.
- **Accurate usage stats** — a snippet's use is counted only after the text is actually injected (not on a failed paste).
- **`{cursor}` in multiline snippets** — caret repositioning now accounts for editors that expand `\n` to `\r\n`.
- **Blank tray icon / missing notifications** — the app icon wasn't copied next to a single-file publish, so the tray showed a blank icon; a `NotifyIcon` with an invalid icon also suppresses balloon tips, which is why conversion/suggestion notifications never appeared. The icon is now embedded in the assembly and loaded at the exact tray size; balloons use an info glyph and a longer timeout.
- **UI refresh** — a card-based layout with an app header (logo + tagline), an elevated sidebar with accent-bar navigation and grouped ghost actions, a search field with a leading glyph, and monospace "chip" triggers in the snippet grid.
- **AI improve selection** (Ctrl+Shift+I, rebindable) — select text and press the hotkey to have an AI clean up spelling/grammar while keeping the same language and meaning, replacing the selection in place. Provider-agnostic behind an `IAiService` port; ships with a **Google Gemini** (free-tier) implementation. **Bring your own API key** — paste it in Settings; with no key the feature is inert. Text is sent to the provider **only** when you invoke it, never automatically, and password fields are skipped. Failures are logged and swallowed (no replacement on error).

### Notes
- Cloud sync and the plugin system are intentionally **not** implemented in the MVP. See `docs/09_Roadmap.md`.
- `{input:Label}` (interactive placeholder prompts) is scaffolded conceptually but deferred — unknown tokens are preserved verbatim, so it's a non-breaking addition later.

[Unreleased]: https://example.com/smarttyping/tree/main
