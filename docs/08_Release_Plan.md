# 08 — Release Plan

## 1. Versioning

Semantic Versioning (`MAJOR.MINOR.PATCH`). The MVP targets **0.1.0**. Version is set in
`Directory.Build.props` and surfaced in the About/tray tooltip.

## 2. Release channels

| Channel | Purpose                          | Cadence          |
|---------|----------------------------------|------------------|
| dev     | local builds, PR validation      | continuous       |
| preview | internal testers                 | per milestone    |
| stable  | end users                        | on milestone GA  |

## 3. Build & publish

Framework-dependent (dev):
```powershell
dotnet build -c Release
```

Self-contained single-file (release candidate):
```powershell
dotnet publish src/SmartTyping.UI -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```
Output: `src/SmartTyping.UI/bin/Release/net9.0-windows/win-x64/publish/`.

## 4. Packaging (MVP)

- Ship the single-file `SmartTyping.exe` from the publish profile (zip optional).
- **winget**: portable manifests live in `packaging/winget/` — fill the release URL + SHA-256, then
  `winget validate` / submit so users can `winget install SmartTyping.SmartTyping`. See that folder's README.
- **Installer (Inno Setup)**: `packaging/installer/SmartTyping.iss` + `scripts/build-installer.ps1`
  produce a per-user `SmartTyping-Setup-<version>.exe` (Start Menu / optional Desktop shortcut,
  uninstaller). Requires Inno Setup 6 (`iscc`). MSIX/Store is deferred (needs a signing cert).

## 4a. Updates

- **In-app update check** (opt-in, off by default — the app's only network feature). When enabled,
  it queries the GitHub "latest release" endpoint on startup and via Settings -> "Check now",
  comparing versions with the pure `UpdateComparer`. If newer, it offers to open the download page.
- Point `WindowsUpdateChecker.LatestReleaseUrl` at the real repository before release.

## 5. Pre-release checklist

- [ ] `dotnet test` green.
- [ ] Manual hook/injection checklist (see Test Plan §4) passes on Win10 + Win11.
- [ ] Version bumped in `Directory.Build.props`.
- [ ] `CHANGELOG.md` updated; `[Unreleased]` moved under the new version + date.
- [ ] Fresh-machine smoke test (no dev tools): DB self-initializes, app runs.
- [ ] Logs contain no unhandled exceptions during smoke.

## 6. Release steps

1. Tag: `git tag v0.1.0 && git push --tags`.
2. Build the self-contained publish artifact.
3. Zip and attach to the release page with `CHANGELOG` excerpt.
4. Smoke-test the artifact on a clean VM.

## 7. Rollback

Artifacts are immutable per tag. To roll back, re-publish the previous tag's zip. User data
(the SQLite DB) is forward/backward compatible within a MINOR line; schema bumps are additive.

## 8. Milestones

| Version | Contents                                                     |
|---------|--------------------------------------------------------------|
| 0.1.0   | MVP: snippets + templates + layout conversion + settings.    |
| 0.2.0   | Configurable hotkeys, start-with-Windows, tray polish.       |
| 0.3.0   | Repository integration tests, import/export snippets.        |
| 1.0.0   | Hardened injection, installer, optional auto-detect (opt-in). |
