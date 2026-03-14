<#
.SYNOPSIS
    Restore and build the solution. No tests.

.PARAMETER Config
    Build configuration. Default: Release

.PARAMETER RestoreOnly
    Only run dotnet restore, skip the build.

.EXAMPLE
    pwsh -File scripts\build.ps1
    pwsh -File scripts\build.ps1 -Config Debug
    pwsh -File scripts\build.ps1 -RestoreOnly
#>
[CmdletBinding()]
param(
    [string]$Config = "Release",
    [switch]$RestoreOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$sln  = Join-Path $root "TheWonderlandSolution.sln"

Write-Host "==> dotnet restore" -ForegroundColor Cyan
dotnet restore $sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "FAILED: restore (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}

if ($RestoreOnly) {
    Write-Host "Restore complete." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "==> dotnet build -c $Config" -ForegroundColor Cyan
dotnet build $sln --no-restore -c $Config
if ($LASTEXITCODE -ne 0) {
    Write-Host "FAILED: build (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Build complete." -ForegroundColor Green
exit 0
