$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$scriptPath = Join-Path $repoRoot 'install-mrc.ps1'

$failures = [System.Collections.Generic.List[string]]::new()
$passes = 0

function Check([string]$name, [scriptblock]$body) {
    try {
        & $body
        Write-Host "PASS  $name"
        $script:passes++
    }
    catch {
        Write-Host "FAIL  $name"
        Write-Host "      $($_.Exception.Message)"
        $script:failures.Add($name)
    }
}

Check 'bootstrap is pinned to exact v0.0.9 release assets' {
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw 'install-mrc.ps1 is missing.'
    }
    $source = Get-Content -LiteralPath $scriptPath -Raw
    foreach ($required in @(
        "`$version = '0.0.9'",
        'releases/download/v$version/MRC-v$version-win-x64.zip',
        'releases/download/v$version/SHA256SUMS.txt'
    )) {
        if (-not $source.Contains($required, [StringComparison]::Ordinal)) {
            throw "Missing pinned release contract: $required"
        }
    }
}

Check 'bootstrap verifies SHA-256 before extraction' {
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw 'install-mrc.ps1 is missing.'
    }
    $source = Get-Content -LiteralPath $scriptPath -Raw
    $hashIndex = $source.IndexOf('Get-FileHash', [StringComparison]::Ordinal)
    $compareIndex = $source.IndexOf('Checksum mismatch', [StringComparison]::Ordinal)
    $extractIndex = $source.IndexOf('Expand-Archive', [StringComparison]::Ordinal)
    if ($hashIndex -lt 0 -or $compareIndex -lt 0 -or $extractIndex -lt 0) {
        throw 'Hash verification/extraction contract is incomplete.'
    }
    if ($hashIndex -gt $extractIndex -or $compareIndex -gt $extractIndex) {
        throw 'Archive extraction occurs before checksum verification.'
    }
    if (-not $source.Contains("-Algorithm SHA256", [StringComparison]::Ordinal)) {
        throw 'SHA-256 is not explicitly required.'
    }
}

Check 'bootstrap invokes existing installer only after verification and cleans temp files' {
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw 'install-mrc.ps1 is missing.'
    }
    $source = Get-Content -LiteralPath $scriptPath -Raw
    $extractIndex = $source.IndexOf('Expand-Archive', [StringComparison]::Ordinal)
    $installIndex = $source.IndexOf("'install.ps1'", [StringComparison]::Ordinal)
    if ($extractIndex -lt 0 -or $installIndex -lt 0 -or $installIndex -lt $extractIndex) {
        throw 'Existing package installer is not invoked after verified extraction.'
    }
    if (-not $source.Contains('finally', [StringComparison]::Ordinal) -or
        -not $source.Contains('Remove-Item', [StringComparison]::Ordinal)) {
        throw 'Temporary bootstrap files are not cleaned in a finally block.'
    }
}

Write-Host ""
if ($failures.Count -gt 0) {
    throw "$($failures.Count) bootstrap test(s) failed: $($failures -join ', ')"
}

Write-Host "PASS  all $passes bootstrap installer tests"
