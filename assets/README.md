# assets

Application assets (icons, images).

- `app.ico` — the application/tray icon: a **lightning bolt** on an indigo→violet rounded square,
  signalling "SmartTyping = fast, smart typing" (the umbrella over language correction and text
  expansion). Multi-resolution (16–256 px). Used as the exe icon (`<ApplicationIcon>`) and copied
  next to the executable so the tray loads it at runtime. If absent, the app falls back to a stock
  Windows icon.
- `app-preview.png` — a 256 px PNG preview of the icon for docs.

To regenerate, run `scripts/generate-icon.ps1` (System.Drawing based).
