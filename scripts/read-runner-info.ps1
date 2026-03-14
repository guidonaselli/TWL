<#
.SYNOPSIS
    Smart lookup for common runner/report files in this repo.
    Knows where test results, logs, artifacts, and server logs live.

.PARAMETER Name
    Partial filename to match (case-insensitive). Optional.

.PARAMETER Type
    Narrow the search scope. One of: trx, log, artifact, server-log
    - trx        : TWL.Tests/TestResults/*.trx
    - log        : *.log (repo root)
    - artifact   : artifacts/*.json
    - server-log : TWL.Server/Logs/*

.PARAMETER Tail
    Number of lines to show from the end. Default: 0 (show all)

.EXAMPLE
    pwsh -File scripts\read-runner-info.ps1 -Type trx
    pwsh -File scripts\read-runner-info.ps1 -Type log -Name "test_"
    pwsh -File scripts\read-runner-info.ps1 -Type log -Name "test.log" -Tail 40
    pwsh -File scripts\read-runner-info.ps1 -Type artifact
    pwsh -File scripts\read-runner-info.ps1 -Type server-log -Tail 100
#>
[CmdletBinding()]
param(
    [string]$Name = "",

    [ValidateSet("trx", "log", "artifact", "server-log", "")]
    [string]$Type = "",

    [int]$Tail = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent

# Search locations per type
$searchSpecs = @{
    "trx"        = @{ Base = "TWL.Tests/TestResults"; Pattern = "*.trx" }
    "log"        = @{ Base = ".";                     Pattern = "*.log" }
    "artifact"   = @{ Base = "artifacts";             Pattern = "*.json" }
    "server-log" = @{ Base = "TWL.Server/Logs";       Pattern = "*" }
}

if ($Type -ne "" -and -not $searchSpecs.ContainsKey($Type)) {
    Write-Host "ERROR: Unknown type '$Type'. Valid: trx, log, artifact, server-log" -ForegroundColor Red
    exit 1
}

# Collect candidates
$candidates = [System.Collections.Generic.List[string]]::new()

$typesToSearch = if ($Type -ne "") { @($Type) } else { $searchSpecs.Keys }

foreach ($t in $typesToSearch) {
    $spec    = $searchSpecs[$t]
    $baseDir = Join-Path $root $spec.Base
    if (-not (Test-Path $baseDir)) { continue }

    Get-ChildItem -Path $baseDir -Filter $spec.Pattern -File |
        Where-Object { $Name -eq "" -or $_.Name -ilike "*$Name*" } |
        ForEach-Object { $candidates.Add($_.FullName) }
}

if ($candidates.Count -eq 0) {
    $hint = if ($Type -ne "") { " (type=$Type)" } else { "" }
    $hint += if ($Name -ne "") { " (name=$Name)" } else { "" }
    Write-Host "No files found$hint." -ForegroundColor Yellow
    exit 1
}

if ($candidates.Count -gt 1) {
    Write-Host "Multiple files found — specify -Name to narrow down:" -ForegroundColor Yellow
    $candidates | ForEach-Object {
        $rel = $_.Substring($root.Length).TrimStart('\', '/')
        Write-Host "  $rel"
    }
    exit 2
}

# Single match — read it
$file = $candidates[0]
$item = Get-Item $file
Write-Host "==> $($item.FullName)  ($([math]::Round($item.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
Write-Host ""

$content = Get-Content $file

if ($Tail -gt 0 -and $content.Count -gt $Tail) {
    Write-Host "--- (showing last $Tail of $($content.Count) lines) ---" -ForegroundColor DarkGray
    $content | Select-Object -Last $Tail | ForEach-Object { Write-Host $_ }
} else {
    $content | ForEach-Object { Write-Host $_ }
}

exit 0
