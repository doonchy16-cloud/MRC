$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$version = '0.0.10'
$targetMachine = 'DOONCHYSCOMPUTI'
$runnerRoot = 'D:\Git_Runners_Main'
$packageFile = "MRC-v$version-win-x64.zip"
$zipUrl = "https://github.com/doonchy16-cloud/MRC/releases/download/v$version/MRC-v$version-win-x64.zip"
$checksumUrl = "https://github.com/doonchy16-cloud/MRC/releases/download/v$version/SHA256SUMS.txt"

if ($env:COMPUTERNAME -ine $targetMachine) {
    throw "MRC is authorized only on $targetMachine. Current machine: $($env:COMPUTERNAME)"
}

if (-not (Test-Path -LiteralPath $runnerRoot -PathType Container)) {
    throw "Authorized runner root does not exist: $runnerRoot"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("mrc-bootstrap-$version-" + [Guid]::NewGuid().ToString('N'))
$zipPath = Join-Path $tempRoot $packageFile
$checksumPath = Join-Path $tempRoot 'SHA256SUMS.txt'
$extractRoot = Join-Path $tempRoot 'package'

try {
    New-Item -ItemType Directory -Force -Path $tempRoot, $extractRoot | Out-Null

    Write-Host "Downloading MRC v$version..."
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath
    Invoke-WebRequest -Uri $checksumUrl -OutFile $checksumPath

    $checksumLine = Get-Content -LiteralPath $checksumPath |
        Where-Object { $_ -match "^([A-Fa-f0-9]{64})\s+\*?$([Regex]::Escape($packageFile))$" } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($checksumLine)) {
        throw "Checksum authority does not contain an entry for $packageFile."
    }

    $expectedHash = ([Regex]::Match($checksumLine, '^([A-Fa-f0-9]{64})')).Groups[1].Value.ToLowerInvariant()
    $actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()

    if ($actualHash -ne $expectedHash) {
        throw "Checksum mismatch for $packageFile. Expected $expectedHash, got $actualHash."
    }

    Write-Host 'SHA-256 verified.'
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractRoot -Force

    $manifestPath = Join-Path $extractRoot 'manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw 'Downloaded package is missing manifest.json.'
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ([string]$manifest.version -ne $version) {
        throw "Downloaded package version mismatch. Expected $version, got $($manifest.version)."
    }

    $installPath = Join-Path $extractRoot 'install.ps1'
    if (-not (Test-Path -LiteralPath $installPath -PathType Leaf)) {
        throw 'Downloaded package is missing install.ps1.'
    }

    Write-Host "Installing MRC v$version..."
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $installPath -PackageRoot $extractRoot
    if ($LASTEXITCODE -ne 0) {
        throw "MRC installer failed with exit code $LASTEXITCODE."
    }

    Write-Host ''
    Write-Host "MRC v$version installed successfully."
    Write-Host 'Open a new PowerShell window, then run: MRC --version'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
