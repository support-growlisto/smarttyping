# 01 — Product Requirements Document (PRD)

## 1. Overview

SmartTyping Desktop is a local Windows app that helps bilingual Thai/English users
(a) fix wrong-keyboard-layout text and (b) expand short triggers into longer templated text.

## 2. Target users

- Thai office workers, students, developers, and support staff who switch between Thai and English.
- Anyone who retypes boilerplate (signatures, phone numbers, addresses, canned replies).

## 3. User stories (MVP)

| ID   | As a…            | I want to…                                              | So that…                                  |
|------|------------------|---------------------------------------------------------|-------------------------------------------|
| US-1 | user             | type `/phone` and have it expand to my phone number     | I don't retype it constantly              |
| US-2 | user             | organize snippets into categories                       | I can find them easily                    |
| US-3 | user             | enable/disable a snippet without deleting it            | I can pause one temporarily               |
| US-4 | user             | see how often each snippet is used                      | I know which ones matter                  |
| US-5 | user             | select gibberish text and press a hotkey to fix layout  | I don't retype after a wrong-layout typo  |
| US-6 | user             | insert `{date}`/`{time}`/`{clipboard}` inside a snippet | snippets stay dynamic                     |
| US-7 | user             | turn snippet expansion or language correction on/off     | I control when the app acts               |

## 4. Functional requirements

### 4.1 Snippet expansion
- FR-1 Create/read/update/delete snippets (trigger, content, category, enabled, usage count).
- FR-2 A trigger is a short token (default prefix `/`, e.g. `/phone`). Triggers are unique.
- FR-3 On explicit action, the matching snippet's resolved content replaces the trigger.
- FR-4 Expansion increments the snippet's usage count and records a usage-history row.
- FR-5 Disabled snippets never expand.

### 4.2 Language converter
- FR-6 Convert a string between Thai Kedmanee and English QWERTY layouts, character by character.
- FR-7 Hotkey `Ctrl+Shift+L` converts the current selection; if no selection, the last typed word.
- FR-8 Conversion is reversible for characters present in the mapping table; unmapped characters pass through unchanged.
- FR-9 Direction is auto-detected (predominantly Thai → convert to EN layout, and vice versa) with a manual override available later.

### 4.3 Template variables
- FR-10 `{date}` → current date (locale short date), `{time}` → current time, `{clipboard}` → current clipboard text.
- FR-10a `{date:FORMAT}` / `{time:FORMAT}` → custom .NET formats (invariant/Gregorian, e.g. `{date:yyyy-MM-dd}`).
- FR-10b `{date+N}` / `{date-N}` → date offset by N days (optionally combined with a format).
- FR-10c `{cursor}` → marks the caret position after insertion.
- FR-10d `{input:Label}` → prompts the user for a value on expansion (same label prompted once; cancel aborts).
- FR-11 Unknown `{...}` tokens are left intact (no crash, no data loss).
- FR-12 Snippets can be exported to and imported from a JSON file (skip or overwrite on trigger conflict).

### 4.4 Settings
- FR-12 Toggle "snippet expansion enabled" and "language correction enabled".
- FR-13 Settings persist in SQLite and load on startup.
- FR-14 (Planned) start-with-Windows, configurable hotkeys.

## 5. Non-functional requirements

- NFR-1 **Local-only**: no network calls in the MVP.
- NFR-2 **Safety**: no automatic replacement without a user action; avoid acting in password fields where detectable.
- NFR-3 **Performance**: snippet lookup and conversion complete in < 50 ms for typical inputs.
- NFR-4 **Reliability**: a failed injection falls back to clipboard paste and never corrupts the DB.
- NFR-5 **Testability**: core logic isolated from Windows APIs and unit-tested.
- NFR-6 **Privacy**: usage history stays local; user can clear it.

## 6. Out of scope (MVP)

Automatic correction, AI, cloud sync, plugins, mobile, macOS/Linux.

## 7. Acceptance criteria (samples)

- Given a snippet `/sig` → "Best regards,\nDana", when the user triggers expansion on `/sig`,
  then the trigger is replaced by the two-line signature and usage count increments by 1.
- Given selected text `l;ylfu`, when the user presses `Ctrl+Shift+L`,
  then the selection becomes `สวัสดี`.
- Given a snippet content `Today is {date}`, when expanded,
  then `{date}` is replaced with the current short date.
