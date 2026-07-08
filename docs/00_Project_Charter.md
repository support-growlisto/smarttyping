# 00 — Project Charter

## Project

**SmartTyping Desktop** — a Windows productivity utility combining Thai/English
language correction with text expansion and templates.

## Vision

Let bilingual (Thai/English) users type faster and fix wrong-keyboard-layout mistakes
without breaking flow or leaving their current application — all locally, with explicit
user control and no automatic destructive edits.

## Problem statement

- Bilingual users frequently type in the wrong keyboard layout (Thai text comes out as
  gibberish English, or vice versa) and must retype.
- Repetitive text (phone numbers, addresses, greetings, boilerplate) is retyped constantly.
- Existing tools are either cloud-bound, subscription-locked, or do aggressive automatic
  replacement that users do not trust.

## Goals (MVP)

1. Convert text between Thai Kedmanee and English QWERTY on an explicit hotkey.
2. Expand short triggers (e.g. `/phone`) into configured text with categories and usage tracking.
3. Support template variables (`{date}`, `{time}`, `{clipboard}`).
4. Keep everything local (SQLite), safe (no auto-replace), and testable.

## Non-goals (MVP)

- Automatic as-you-type correction.
- AI-assisted features.
- Cloud sync / multi-device.
- Plugin ecosystem.
- Manipulating password fields aggressively.

## Success criteria

- A user can create, edit, enable/disable, and categorize snippets.
- `Ctrl+Shift+L` converts the current selection / last word between layouts correctly for common cases.
- Snippet expansion inserts the resolved text (including template variables) into the active app.
- Core logic (conversion, snippet lookup, template replacement) is covered by passing unit tests.
- App runs on a clean Windows 10/11 machine with the .NET 9 runtime.

## Stakeholders

| Role            | Responsibility                                  |
|-----------------|-------------------------------------------------|
| Product owner   | Scope, priorities, acceptance                   |
| Developer(s)    | Implementation, tests, docs                     |
| End users       | Bilingual Thai/English typists on Windows       |

## Constraints

- Windows 10/11 only; .NET 9; C#/WPF/MVVM.
- Local-first: no network calls in the MVP.
- Explicit-action model: no silent global replacement.

## High-level timeline

| Phase | Outcome                                                        |
|-------|---------------------------------------------------------------|
| 0     | Repository skeleton, docs, architecture (this delivery).      |
| 1     | Snippet CRUD + expansion + template variables (usable).       |
| 2     | Language converter hotkey end-to-end.                          |
| 3     | Settings, tray polish, packaging.                              |

## Risks

- Global keyboard hooks / text injection are OS-sensitive and app-dependent — mitigate by
  isolating them behind interfaces and shipping a clipboard-paste fallback.
- Layout conversion edge cases (dead keys, shifted symbols) — mitigate with a well-tested mapping table.
