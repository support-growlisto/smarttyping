# ADR-002 — Keyboard hook & global hotkey strategy

- Status: Accepted
- Date: 2026-07-08

## Context

The MVP needs a global hotkey (`Ctrl+Shift+L`) to trigger language conversion, and a path to
capture the "last typed word" and inject converted/expanded text into the foreground app. These
are Windows-specific, security-sensitive, and app-dependent. We must not build automatic
as-you-type correction yet, and we must degrade gracefully.

## Decision

1. **Two mechanisms, both isolated in Infrastructure behind interfaces:**
   - `RegisterHotKey` (Win32) for the fixed global hotkey — cheap, reliable, no per-keystroke cost.
   - A low-level keyboard hook (`WH_KEYBOARD_LL`) abstraction `IKeyboardHook` for capturing the
     last-typed-word buffer. In the MVP the hook is **passive** (observe only) — it never rewrites input.
2. **No automatic replacement.** Conversion/expansion only happens on the explicit hotkey / user action.
3. **Text acquisition & injection are ports:** `IClipboardService` and `ITextInjector`. Injection
   prefers direct simulated typing but **always** falls back to clipboard paste; the converted text
   stays on the clipboard so the user can paste manually if injection is blocked.
4. **Safety:** where a secure/password field is detectable, skip acting. All hook/injection code
   logs-and-continues; a hook exception must never crash the app.

## Consequences

- **Pro**: business logic is testable with fakes; OS quirks are contained; users keep control.
- **Pro**: clipboard fallback means the feature is useful even where synthetic input is blocked.
- **Con**: capturing selection via copy briefly touches the clipboard. We snapshot and restore the
  **entire** clipboard (all formats) around the operation, so images/files/rich text are preserved.
- **Con**: low-level hooks can be flagged by some security software; documented for users.

## Secure-field handling (implemented)

Before capturing a selection or injecting text, the coordinators query `ISecureInputDetector`. The
Windows implementation resolves the foreground window's focused control via `GetGUIThreadInfo` and
checks the `ES_PASSWORD` style; if set, the action is skipped (nothing is copied or pasted). This is
**best-effort**: it reliably covers native Win32 `Edit` password boxes, but browsers, Electron, and
UWP apps do not expose that style, so their password fields cannot be detected. A `false` result
therefore means "not known to be secure", not a guarantee. Broader coverage (e.g. UI Automation
`IsPassword`) is a future enhancement.

## Alternatives considered

- **UI Automation to read selection** — cleaner in theory but inconsistent across apps. Kept as a
  future enhancement, not the MVP path.
- **Always inject by simulated keystrokes** — fails in apps that block `SendInput`; clipboard paste
  is the more reliable baseline. We do both, paste-first as fallback.
- **Active hook that rewrites keys inline** — that is automatic correction, explicitly out of scope.
