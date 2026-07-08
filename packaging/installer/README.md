# Windows installer (Inno Setup)

Builds a classic `SmartTyping-Setup-<version>.exe` installer that installs the self-contained
single-file app, creates Start Menu (and optional Desktop) shortcuts, and registers an uninstaller.

## Why Inno (not MSIX) for now

- No code-signing certificate required to produce a working installer.
- Per-user install (`PrivilegesRequired=lowest`) — no admin prompt.
- Simple, well-understood, and pairs with the in-app "start with Windows" and update-check options.

MSIX/Store packaging can be added later (it needs a signing cert and a Store/enterprise flow).

## Prerequisites

- .NET 9 SDK (to publish the app).
- [Inno Setup 6](https://jrsoftware.org/isdl.php) — `iscc.exe` on PATH, or `winget install JRSoftware.InnoSetup`.

## Build

```powershell
pwsh scripts/build-installer.ps1 -Version 0.1.0
```

This publishes `src/SmartTyping.UI` (self-contained, single file, win-x64) and compiles
`SmartTyping.iss`, writing the installer to `dist/`.

To compile the script directly (after publishing):

```powershell
iscc /DAppVersion=0.1.0 packaging/installer/SmartTyping.iss
```

## Notes

- `PublishDir` defaults to the win-x64 publish output; override with `iscc /DPublishDir=...` if needed.
- The app ships `assets/app.ico` next to the exe; the installer includes the `assets` folder if present.
- Bump `-Version` per release so the installer filename and Add/Remove Programs entry match the app version.
