<#
.SYNOPSIS
    Read any file (log, txt, md, trx, json) and display its contents.

.PARAMETER Path
    Path to the file. Absolute or relative to repo root.

.PARAMETER Tail
    Number of lines to show from the end. Default: 0 (show all)

.EXAMPLE
    pwsh -File scripts\read-log.ps1 -Path test.log
    pwsh -File scripts\read-log.ps1 -Path test.log -Tail 50
    pwsh -File scripts\read-log.ps1 -Path TWL.Tests/TestResults/results.trx
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Path,

    [int]$Tail = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent

# Resolve: try as-is, then relative to repo root
$resolved = $Path
if (-not [System.IO.Path]::IsPathRooted($Path)) {
    $resolved = Join-Path $root $Path
}

if (-not (Test-Path $resolved)) {
    Write-Host "ERROR: File not found: $resolved" -ForegroundColor Red
    exit 1
}

$item = Get-Item $resolved
Write-Host "==> $($item.FullName)  ($([math]::Round($item.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
Write-Host ""

$content = Get-Content $resolved

if ($Tail -gt 0 -and $content.Count -gt $Tail) {
    Write-Host "--- (showing last $Tail of $($content.Count) lines) ---" -ForegroundColor DarkGray
    $content | Select-Object -Last $Tail | ForEach-Object { Write-Host $_ }
} else {
    $content | ForEach-Object { Write-Host $_ }
}

exit 0
