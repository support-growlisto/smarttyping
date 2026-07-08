# winget packaging

Manifest scaffolding to publish SmartTyping to the [Windows Package Manager](https://learn.microsoft.com/windows/package-manager/) so users can:

```powershell
winget install SmartTyping.SmartTyping
```

## Files

| File | Purpose |
|------|---------|
| `SmartTyping.SmartTyping.yaml` | Version manifest |
| `SmartTyping.SmartTyping.installer.yaml` | Installer (portable exe) — **fill URL + SHA-256** |
| `SmartTyping.SmartTyping.locale.en-US.yaml` | Metadata (name, description, tags) |

## Release steps

1. Publish the single-file exe:
   ```powershell
   dotnet publish src/SmartTyping.UI -c Release /p:PublishProfile=win-x64
   ```
2. Upload `SmartTyping.exe` to a public URL (e.g. a GitHub release asset).
3. Compute its hash and paste both values into `*.installer.yaml`:
   ```powershell
   (Get-FileHash .\SmartTyping.exe -Algorithm SHA256).Hash
   ```
4. Bump `PackageVersion` in all three files to match the release.
5. Validate and test locally:
   ```powershell
   winget validate --manifest packaging/winget
   winget install --manifest packaging/winget
   ```
6. Submit via a PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) (or use `wingetcreate`).

> Note: the `InstallerUrl`/`InstallerSha256` in the committed manifest are placeholders — winget
> validation will fail until they point at a real, hosted release asset.
