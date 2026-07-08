#!/usr/bin/env pwsh
# Publishes the self-contained single-file app and builds the Windows installer with Inno Setup.
#
# Requires Inno Setup 6 (iscc.exe) on PATH or at the default install location:
#   https://jrsoftware.org/isdl.php   (or: winget install JRSoftware.InnoSetup)
#
# Usage: pwsh scripts/build-installer.ps1 [-Version 0.1.0]

param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Publishing self-contained single file (win-x64)..." -ForegroundColor Cyan
dotnet publish (Join-Path $root "src\SmartTyping.UI\SmartTyping.UI.csproj") -c Release /p:PublishProfile=win-x64

# Locate iscc.exe.
$iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    foreach ($p in @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
        "${env:ProgramFiles}\Inno Setup 6\iscc.exe")) {
        if (Test-Path $p) { $iscc = $p; break }
    }
}
if (-not $iscc) {
    throw "iscc.exe (Inno Setup 6) not found. Install it (winget install JRSoftware.InnoSetup) and retry."
}

Write-Host "Compiling installer with $iscc ..." -ForegroundColor Cyan
& $iscc "/DAppVersion=$Version" (Join-Path $root "packaging\installer\SmartTyping.iss")

Write-Host "Installer written to $(Join-Path $root 'dist')" -ForegroundColor Green
