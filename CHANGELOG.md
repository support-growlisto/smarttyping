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

## [0.6.1]

### Fixed: `้ำ` left behind when correcting a word typed on the Thai layout

Typing `hello` with the Thai layout on left `้ำhello` in Chrome, VS Code and any other modern app.

Windows was never the one rejecting a floating tone mark — the *text control* was. Measured by typing
the same keys into two controls with SmartTyping stopped: a Win32 EDIT control (WinForms, Notepad)
inserts only `สสน`, while a control that renders its own text (WPF, and by extension Chrome and
Electron) inserts all five characters of `้ำสสน`. `ThaiInput.Filter` predicted the first, so in the
second we deleted three characters where five had landed, and the two stragglers survived the fix.

The hook no longer predicts what the application kept. It **enforces** the rule instead: a keystroke
that would produce an impossible sequence is swallowed before it reaches the application, so every app
holds exactly the same text and the count is a fact rather than a guess.

That raises a question the hook cannot answer alone. The very first keystroke after the caret moves
somewhere we did not watch — a click, a navigation key, another window — may be a stray mark to
suppress, or the mark that completes `พันธุ` into `พันธุ์`. Suppressing it either way would silently eat
a keystroke the user meant. So the hook now asks **UI Automation** what sits before the caret, the
moment focus changes (via a `WinEvent` hook, off the keyboard-hook thread) and long before the user
types. When the answer never comes — an app with no text pattern — it tracks nothing rather than
guessing, and no text is touched.

Verified against the running app: `hello` on the Thai layout now yields `hello` in **both** a Win32 edit
control and a WPF one; the thanthakhat typed straight after `พันธุ` completes the word; the same
keystroke into an empty field, or after a space, is suppressed.

## [0.6.0]

### Changed: the hook now takes the keystroke that triggers a replacement

Both automatic features — layout correction and snippet expansion — replace text by backspacing over
what you just typed. But the keystroke that triggers them is still travelling: a low-level keyboard
hook runs *before* the character reaches the target window. The old code papered over that with
`await Task.Delay(35)` — a guess. Type faster than the guess, or land on a busy machine, and the
backspaces arrived before the character did, so the replacement ate the wrong text.

The hook now **swallows** the triggering keystroke instead of waiting for it. The character never
reaches the document, nothing is in flight, and we type the whole result ourselves in one atomic
`SendInput`. There is no race left to lose. This is how text expanders have always done it.

Consequences, all of them deliberate:

- The hook decides *synchronously* whether a replacement is coming, because it must swallow the key
  before letting it through. Snippet expansion therefore consults a new in-memory `IsKnownTrigger`
  predicate; an ordinary word's space is never held hostage by a database lookup that will find nothing.
- The payloads (`LayoutCorrection`, `WordBoundary`) now carry `CharsToDelete` — counted by the hook,
  the only place that knows both what it swallowed and that the Thai layout silently rejects some
  keys — and `SwallowedText`. Handlers no longer do that arithmetic.
- If a replacement turns out not to happen (a secure field, no matching snippet, a failed injection),
  the handler types `SwallowedText` back. Swallowing a key means owning it.
- At a word boundary, only one of expansion and correction may fire; an expansion wins, because the
  user typed a trigger deliberately.

Verified end-to-end against the running app at 8 ms and 4 ms per keystroke — cadences at which the old
delay corrupted text — including negative controls (an ordinary word and a non-trigger keep their space).

### Changed: the remaining guessed sleeps are gone too

The hotkey path (select → convert/expand → inject) had three more fixed delays. None of them survived
contact with the question "what are we actually waiting for?".

- **Selection capture** slept 40 ms after Ctrl+Shift+Left "so the selection would happen". The sentinel
  is now placed on the clipboard *before* any key is sent, and the two keystrokes are queued in order —
  the target cannot process the Ctrl+C before the Ctrl+Shift+Left that precedes it. Where a copy still
  fails to land, we no longer time out and report "nothing selected": we re-send Ctrl+C until the
  clipboard actually changes, which is the only real signal that the copy happened.
- **Text injection** slept 80 ms "to let the target read the clipboard" before restoring it. No amount
  of polling can observe another process reading the clipboard, so the sleep could never be made
  correct — apps that read it late got the *restored* contents, which is how injected text used to
  vanish. The injector now types the text as Unicode keystrokes, exactly as the inline replacer does:
  no clipboard, no snapshot/restore, nothing to race. Your clipboard is no longer touched at all.
- **Quick picker** slept 120 ms after asking Windows to refocus the app you came from.
  `SetForegroundWindow` only *asks*; the activation lands later. It now waits until that window really
  is the foreground window, and if it never becomes one it types nothing rather than dropping your
  snippet into whatever happened to be in front.

One consequence of typing instead of pasting: a converted string that begins with a Thai tone mark or
attached vowel loses those marks, because Windows rejects them with no base consonant — the same rule
`ThaiInput.Filter` already models, and exactly what you would have got typing the characters yourself.

### Fixed: the Thai input model silently mangled 212 real words

`ThaiInput.Filter` predicts which keystrokes the Thai layout will actually accept — the hook counts on
it to know how many characters sit behind the caret. It classified the thanthakhat `์` and the nikhahit
`ํ` as vowels that must attach directly to a consonant, so it "predicted" that `สิทธิ์` and `พันธุ์`
lose their final mark. They do not: those marks stack on top of a vowel, which is how the words are
typed. It also rejected `ะ` after the spacing vowel `า` (`กระเจาะ`, `เคราะห์`) and the paiyannoi that
begins `ฯลฯ`.

Rather than reason about it further, the rules were measured — by typing each sequence into a real text
box and reading back what Windows kept. `ะ`, `ๅ`, `ๆ` and `ฯ` stand alone and are accepted anywhere,
even first (so `ะ้ำ` leaves `ะ`, not nothing, as the old model claimed); sara am needs a base; tones and
thanthakhat may sit on a consonant or on a vowel already resting on one. A test now asserts that all
60,537 words of the embedded dictionary survive the filter unchanged, save a handful of entries in the
source list that carry a vowel written on another vowel — typos in the data, which the layout rejects
exactly as it should.

### Fixed: Ctrl+Shift+Left selected nothing, so "convert the last word" did nothing

Arrow keys are *extended* keys. Sent through `SendInput` without `KEYEVENTF_EXTENDEDKEY` they arrive as
their numeric-keypad twins. A bare Left still moves the caret, which is why this hid for so long — but
Ctrl+Shift+Left arrived as Ctrl+Shift+Numpad4 and selected nothing. So pressing the conversion hotkey
with no selection captured an empty string and silently gave up: the documented "convert the word I just
typed" (FR-7) had never worked. Measured on a real text box: without the flag `SelectedText` was `""`,
with it, `"hello"`. All synthetic navigation keys now carry the flag.

## [0.5.1]

### Fixed: an over-long run could be rewritten at the wrong place
The as-you-type word buffer held 48 characters and then *silently dropped* every further character.
The buffer stayed at 48 while the document kept growing, so the two disagreed about how much text sat
behind the caret. Finish such a run with a space and, if its first 48 characters happened to spell a
word, the corrector would backspace 49 characters from the caret — deleting the tail of the run rather
than the word it thought it had matched, and pasting the conversion in its place. Backspacing past the
start of the buffer desynchronised it the same way.

A run that outgrows the buffer is now **abandoned** rather than truncated: tracking stops until the
next real word break (space, navigation key, mouse click, window switch). Losing track of the caret is
the one situation where doing nothing is the only safe move.

### A word has a maximum length
`LayoutDecider` already refused runs shorter than three characters; it now also refuses anything longer
than twenty. Nothing in either dictionary is longer (English is filtered to 12 characters, Thai to 20),
and a long uninterrupted run is far more likely to be a password, a token or a URL — exactly the text an
automatic rewrite must never touch. The bound is inclusive, so a legitimate twenty-character Thai word
still corrects, and an integration test asserts the cap hides no word we could have matched.

## [0.5.0]

### A typo no longer hides the language
Dictionary lookup was exact, so one slipped key meant no correction at all: `l;yldu` (สวัสกี — `d`
instead of `f`) was simply left as latin. Near-misses now count, scored by where the keys sit on the
keyboard: a neighbouring key is a plausible slip (15), the wrong shift state is (40), the row above or
below is (70), and anything else is (100) — not a typo, a different word.

This is the change with the most room to misfire, so it is fenced in on three sides:

1. **Only at a word boundary.** Mid-word the text is a prefix of something longer, and a near-miss
   there would fire against a word the user is still typing. Mid-word matching stays exact.
2. **A typo is never silently spell-corrected.** The fuzzy match only answers *"which language did
   they mean?"*. The text we type back is the literal transliteration of the keys actually pressed —
   `l;yldu ` becomes `สวัสกี `, not `สวัสดี `. We fix the layout, never the word.
3. **The budget cannot buy a different word.** It is `min(12 × length, 75)`, and that ceiling is below
   the cost of a single unrelated substitution (100). So a long word never accumulates enough
   allowance to match something else, and a transposition (`wrold` for `world`, two unrelated
   substitutions) is rejected outright.

The vetoes stay exact: real Thai is never rewritten because its latin form happens to sit one key away
from an English word.

Verified against the real dictionaries, not a stub — twelve common English words (`hello`, `world`,
`system`, `commit`, `branch`…) survive a fuzzy scan of all 60,537 Thai words. The scan runs inside the
low-level keyboard hook, which Windows unhooks if it is slow, so it is bucketed by word length and
abandons a candidate the moment the budget is blown: a worst-case miss measures **1.5 ms**, only ever
on a space, against Windows' 300 ms timeout.

## [0.4.0]

### Never type on your behalf in the wrong app
The automatic features work by synthesizing backspaces and characters into whatever has focus. That
is fine in a text field and destructive elsewhere, so they now stay silent in a configurable list of
apps (Settings → "Never type in these apps"):

- **terminals** — a stray character lands on a command line and may run;
- **remote desktop / VMs** — the keystrokes are forwarded to another machine, where our idea of what
  is on screen is simply wrong;
- **password managers** — never touch a vault field;
- **games and launchers** — they read raw key state, and synthetic input often reads as cheating.

Explicit hotkeys keep working everywhere: pressing one is a deliberate act, unlike typing a word and
having the app rewrite it. An empty list restores the defaults rather than blocking nothing, and an
unidentifiable foreground process is never treated as blocked — we must not silently disable
ourselves because a window could not be inspected.

Matching ignores path, case and the `.exe` suffix, so `C:\Windows\System32\cmd.exe` matches `cmd`,
while `cmder` and `xcmd` do not.

The check runs on the keyboard hook for every keystroke, and a low-level hook that is too slow gets
removed by Windows. The process name is therefore cached against the foreground window handle: it is
only looked up when you switch windows.

## [0.3.0]

### Undo a correction — and it learns from it
`Shift+Backspace` right after an automatic layout correction puts back exactly what you typed,
restores your keyboard layout, **and remembers the word so it is never corrected again**. The two
features are the same mechanism: undoing is how you teach the app.

Learning needs no new logic in the decider — it drops straight into the existing veto:

- corrected latin → Thai? the latin joins the **English** vocabulary → the "not an English word"
  veto now blocks it.
- corrected Thai → English? the Thai that was on screen joins the **Thai** vocabulary → the "not a
  Thai word" veto now blocks it.

Learned words persist in a `learned_words` table and load with the dictionaries at startup. This is
what makes names, jargon and brand words usable — a fixed dictionary can never know them.

The undo binding is guarded so it cannot break ordinary typing:

- the hook only claims the keystroke while a correction is genuinely the last thing that happened —
  any other key, or a mouse click, disarms it, so `Shift+Backspace` keeps deleting characters normally;
- when it does fire, the keystroke is **swallowed**, so the app never receives a stray Backspace on
  top of the restored text;
- a bare modifier press does not disarm it (Shift goes down before `Shift+Backspace`).

`Hotkey.IsValid` previously demanded Ctrl/Alt/Win. It now also accepts Shift plus a key that types
nothing (Backspace, Delete, F-keys, arrows…), so `Shift+Backspace` is bindable while `Shift+A` is
still rejected. Fixed a latent round-trip bug while there: keys such as Backspace serialized as
`0x08` and could not be parsed back, so a rebind would silently revert to the default.

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
