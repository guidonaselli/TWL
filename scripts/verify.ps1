<#
.SYNOPSIS
    Full CI pipeline: restore → build → test. Fails fast on any step.

.PARAMETER Config
    Build configuration. Default: Release

.EXAMPLE
    pwsh -File scripts\verify.ps1
    pwsh -File scripts\verify.ps1 -Config Debug
#>
[CmdletBinding()]
param(
    [string]$Config = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$sln  = Join-Path $root "TheWonderlandSolution.sln"
$test = Join-Path $root "TWL.Tests/TWL.Tests.csproj"

function Step([string]$label, [scriptblock]$cmd) {
    Write-Host ""
    Write-Host "==> $label" -ForegroundColor Cyan
    & $cmd
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "FAILED: $label (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Step "dotnet restore" {
    dotnet restore $sln
}

Step "dotnet build" {
    dotnet build $sln --no-restore -c $Config
}

Step "dotnet test" {
    dotnet test $test --no-build -c $Config
}

Write-Host ""
Write-Host "All steps passed." -ForegroundColor Green
exit 0
