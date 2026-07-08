#!/usr/bin/env pwsh
# Build helper for SmartTyping Desktop.
# Usage: pwsh scripts/build.ps1 [-Configuration Release] [-Test]

param(
    [string]$Configuration = "Debug",
    [switch]$Test
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Restoring..." -ForegroundColor Cyan
dotnet restore (Join-Path $root "SmartTyping.sln")

Write-Host "Building ($Configuration)..." -ForegroundColor Cyan
dotnet build (Join-Path $root "SmartTyping.sln") -c $Configuration --no-restore

if ($Test) {
    Write-Host "Testing..." -ForegroundColor Cyan
    dotnet test (Join-Path $root "SmartTyping.sln") -c $Configuration --no-build
}

Write-Host "Done." -ForegroundColor Green
