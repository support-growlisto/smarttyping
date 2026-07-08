# 09 — Roadmap

A living view of where SmartTyping Desktop is headed. Order is by priority, not fixed dates.

## Now — MVP (v0.1)

- [x] Clean Architecture skeleton + docs.
- [ ] Snippet CRUD, categories, enable/disable, usage count.
- [ ] Snippet expansion (explicit action) with template variables `{date}`, `{time}`, `{clipboard}`.
- [ ] Thai Kedmanee ↔ English QWERTY conversion via `Ctrl+Shift+L`.
- [ ] Settings toggles (expansion, language correction) persisted in SQLite.
- [ ] Unit tests for converter, template engine, snippet lookup.
- [ ] System tray shell.

## Next (v0.2)

- ~~Configurable hotkeys (rebind the conversion/expansion keys)~~ — **done** (v0.1): all four hotkeys rebindable in Settings.
- ~~Start with Windows (registry/Startup entry, user opt-in)~~ — **done** (v0.1).
- ~~User-input placeholders in templates (`{input:Label}` prompts a small dialog)~~ — **done** (v0.1).
- ~~Import/export snippets (JSON)~~ — **done** (v0.1): skip/overwrite conflict handling, category recreation.
- ~~Preview / test-expand and richer template variables (`{date:fmt}`, `{date+N}`, `{cursor}`)~~ — **done** (v0.1).
- ~~Convert the *last typed word* without a selection~~ — **done** (v0.1): auto-selects the previous word.
- ~~Snippet quick-picker / command palette (fuzzy search + insert)~~ — **done** (v0.1): Ctrl+Shift+Space.
- Better direction detection and a manual "convert to Thai / to English" choice.

## Recently shipped (v0.1, beyond the original MVP)

- Dark mode (follow-system + manual), Thai/English localization.
- `{input:Label}` placeholders, `{cursor}`, date/time formats & offsets.
- Quick-picker (fuzzy search), convert-last-word, add-snippet-from-selection.
- Configurable hotkeys, category management, usage statistics.
- Import/Export, onboarding, start-with-Windows, single-instance, secure-field guard.

## Later (v0.3–v1.0)

- Repository integration tests + migration framework hardening.
- Reliable text injection across more apps; broaden secure-field detection beyond native `Edit`
  password boxes (already skipped) to browsers/Electron/UWP via UI Automation `IsPassword`.
- Snippet search improvements (fuzzy, recent, favorites).
- Optional **opt-in** auto-expand as you type (still never destructive without inline confirmation).
- ~~Installer (Inno Setup), auto-update~~ — **done** (v0.1): per-user installer + opt-in update check. MSIX/Store still later.
- Cloud sync + accounts — deferred (needs a hosted backend + OAuth); see the charter's non-goals.

## Deferred — explicitly out of scope until later

These are intentionally **not** built yet (see constraints in the charter):

- **Automatic as-you-type correction** — MVP is hotkey/manual only.
- **AI-assisted correction / suggestions** — no AI in the MVP.
- **Cloud sync / multi-device** — local-first only.
- **Plugin system** — see ADR-004; core stays lean first.
- **macOS / Linux** — Windows-only for now.

## Guiding constraints (carry forward)

- Never do global destructive replacement without an explicit user action.
- Keep everything local and private by default.
- Keep core logic pure and tested; keep OS-specific code behind interfaces.
