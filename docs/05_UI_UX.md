# 05 — UI / UX

## 1. Principles

- **Explicit, not surprising** — nothing replaces text without a user action in the MVP.
- **Stay out of the way** — lives mostly in the tray; the main window is a manager, not a constant presence.
- **Bilingual-friendly** — Thai and English labels read cleanly; fonts render Thai correctly.

## 2. Windows & views

### 2.1 Main window (Snippet Manager)
```
┌──────────────────────────────────────────────────────────┐
│ SmartTyping                                   [_] [□] [x] │
├──────────────┬───────────────────────────────────────────┤
│ Categories   │  [ Search… ]            [+ Add] [Edit] [x] │
│  • All       │  ┌─────────────────────────────────────┐  │
│  • General   │  │ Trigger │ Content (preview) │ Uses │✓│  │
│  • Work      │  │ /sig    │ Best regards, …   │  12  │☑│  │
│  • …         │  │ /phone  │ 08x-xxx-xxxx      │   4  │☑│  │
│              │  │ /date   │ Today is {date}   │   0  │☐│  │
│  [Settings]  │  └─────────────────────────────────────┘  │
└──────────────┴───────────────────────────────────────────┘
```
- Left: category list (with "All"). Selecting filters the grid.
- Center: searchable snippet grid — Trigger, Content preview, Usage count, Enabled checkbox.
- Toolbar: Add, Edit, Delete, Search.
- Bottom-left: Settings button.

### 2.2 Add/Edit snippet dialog
```
┌── Add Snippet ───────────────────────────┐
│ Trigger:  [ /phone            ]          │
│ Category: [ General        ▼ ]           │
│ Enabled:  [x]                            │
│ Content:                                 │
│ ┌──────────────────────────────────────┐ │
│ │ 08x-xxx-xxxx                          │ │
│ │                                       │ │
│ └──────────────────────────────────────┘ │
│ Variables: {date} {time} {clipboard}     │
│                      [ Cancel ] [ Save ] │
└──────────────────────────────────────────┘
```
- Validates: trigger non-empty and unique; content non-empty.
- Shows available template variables as hint chips.

### 2.3 Settings view
```
┌── Settings ──────────────────────────────┐
│ [x] Enable snippet expansion             │
│ [x] Enable language correction           │
│ Language hotkey:  Ctrl + Shift + L (fixed)│
│ [ ] Start with Windows        (planned)  │
│                              [ Close ]   │
└──────────────────────────────────────────┘
```

### 2.4 System tray
- Icon in the notification area.
- Context menu: **Show manager**, **Snippet expansion ✓**, **Language correction ✓**, **Exit**.
- Left-click: show/hide the main window. Closing the main window hides to tray (does not exit).

## 3. Interaction model

| Action                     | Trigger                                  |
|----------------------------|------------------------------------------|
| Expand snippet             | Select the trigger (e.g. `/phone`), press `Ctrl + Shift + E` |
| Convert layout             | `Ctrl + Shift + L` on the selection       |
| Open manager               | Tray left-click / menu                   |
| Toggle a feature           | Tray menu or Settings                    |

## 4. Visual style

- WPF default theme, light; system accent where available.
- Font: Segoe UI + a Thai-capable fallback (Windows renders Thai via font fallback).
- Minimum window size 720×480; grid rows comfortable for multiline content preview (single-line, ellipsis).

### 4.1 Localization

- UI language is **Thai by default**, switchable to English live from Settings (persisted in `app_settings`).
- All window text, the tray menu, and status/dialog messages are localized through `LocalizationManager`
  (`src/SmartTyping.UI/Localization/`), which XAML consumes via an indexer binding
  (`{Binding [Key], Source={x:Static loc:LocalizationManager.Instance}}`).
- Only UI strings are translated; date/number formatting continues to follow the system culture.
- Adding a language = extend the string table with a new column; adding a string = one table entry + its binding.

## 5. Accessibility & feedback

- All actions reachable by keyboard; standard tab order.
- Toast/statusbar confirmation after expansion or conversion ("Converted", "Expanded /sig").
- Errors surface as non-blocking status messages, never silent failures.

## 6. Empty & error states

- No snippets → friendly empty state with an "Add your first snippet" call to action.
- Injection blocked (secure field) → status message suggesting manual paste; clipboard still holds the result.
