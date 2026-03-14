<#
.SYNOPSIS
    Run dotnet test with a filter and show the last N lines of output.

.PARAMETER Filter
    Raw xUnit filter expression (single string). Use this for simple cases.
    Example: -Filter "FullyQualifiedName~CharacterRebirthTransactionTests"

.PARAMETER Names
    One or more class/method name fragments, joined automatically with OR.
    Avoids pipe characters in the command line.
    Example: -Names RebirthEndToEnd,RebirthRollbackAudit,PetRebirthIntegration

.PARAMETER Category
    Shorthand for -Filter "Category=<value>".
    Example: -Category Rebirth

.PARAMETER Project
    Path to the test .csproj. Default: TWL.Tests/TWL.Tests.csproj

.PARAMETER NoBuild
    Pass --no-build to dotnet test.

.PARAMETER Tail
    Number of lines to show from the end of output. Default: 50 (0 = all)

.PARAMETER Config
    Build configuration. Default: Release

.EXAMPLE
    # Single class
    pwsh -File scripts\test-filter.ps1 -Names CharacterRebirthTransactionTests -NoBuild -Tail 30

    # Multiple classes — no pipes needed in the command line
    pwsh -File scripts\test-filter.ps1 -Names RebirthEndToEnd,RebirthRollbackAudit,PetRebirthIntegration -NoBuild -Tail 30

    # By category
    pwsh -File scripts\test-filter.ps1 -Category Rebirth -NoBuild

    # Raw filter for advanced expressions
    pwsh -File scripts\test-filter.ps1 -Filter "FullyQualifiedName~CharacterRebirthTransactionTests" -NoBuild -Tail 30
#>
[CmdletBinding(DefaultParameterSetName = "Names")]
param(
    [Parameter(ParameterSetName = "Names")]
    [string[]]$Names,

    [Parameter(ParameterSetName = "Raw")]
    [string]$Filter,

    [Parameter(ParameterSetName = "Category")]
    [string]$Category,

    [string]$Project = "TWL.Tests/TWL.Tests.csproj",

    [switch]$NoBuild,

    [int]$Tail = 50,

    [string]$Config = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root     = Split-Path $PSScriptRoot -Parent
$projPath = Join-Path $root $Project

if (-not (Test-Path $projPath)) {
    Write-Host "ERROR: Project not found: $projPath" -ForegroundColor Red
    exit 1
}

# Build filter expression from whichever parameter set was used
$filterExpr = switch ($PSCmdlet.ParameterSetName) {
    "Names" {
        if (-not $Names -or $Names.Count -eq 0) {
            Write-Host "ERROR: Provide at least one name via -Names." -ForegroundColor Red
            exit 1
        }
        # Join multiple names with xUnit OR operator (built inside the script, no pipes on CLI)
        ($Names | ForEach-Object { "FullyQualifiedName~$_" }) -join "|"
    }
    "Category" {
        "Category=$Category"
    }
    "Raw" {
        $Filter
    }
}

$dotnetArgs = @(
    "test", $projPath,
    "--filter", $filterExpr,
    "-c", $Config
)
if ($NoBuild) { $dotnetArgs += "--no-build" }

Write-Host "==> dotnet $($dotnetArgs -join ' ')" -ForegroundColor Cyan
Write-Host ""

$proc = Start-Process -FilePath "dotnet" -ArgumentList $dotnetArgs `
    -RedirectStandardOutput "$env:TEMP\_twl_test_out.txt" `
    -RedirectStandardError  "$env:TEMP\_twl_test_err.txt" `
    -NoNewWindow -Wait -PassThru

$outLines = if (Test-Path "$env:TEMP\_twl_test_out.txt") { Get-Content "$env:TEMP\_twl_test_out.txt" } else { @() }
$errLines = if (Test-Path "$env:TEMP\_twl_test_err.txt") { Get-Content "$env:TEMP\_twl_test_err.txt" } else { @() }

$lines = [System.Collections.Generic.List[string]]::new()
$outLines | ForEach-Object { $lines.Add($_) }
$errLines | ForEach-Object { $lines.Add("[stderr] $_") }

if ($Tail -gt 0 -and $lines.Count -gt $Tail) {
    Write-Host "--- (showing last $Tail of $($lines.Count) lines) ---" -ForegroundColor DarkGray
    $lines | Select-Object -Last $Tail | ForEach-Object { Write-Host $_ }
} else {
    $lines | ForEach-Object { Write-Host $_ }
}

Remove-Item "$env:TEMP\_twl_test_out.txt", "$env:TEMP\_twl_test_err.txt" -ErrorAction SilentlyContinue

$exitCode = $proc.ExitCode
Write-Host ""
if ($exitCode -ne 0) {
    Write-Host "Tests FAILED (exit $exitCode)" -ForegroundColor Red
} else {
    Write-Host "Tests PASSED" -ForegroundColor Green
}
exit $exitCode
