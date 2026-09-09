[CmdletBinding()]
param(
    [string]$PackageRoot = $PSScriptRoot,
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'MRC')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$targetMachine = 'DOONCHYSCOMPUTI'
$runnerRoot = 'D:\Git_Runners_Main'

if ($env:COMPUTERNAME -ine $targetMachine) {
    throw "MRC is authorized only on $targetMachine. Current machine: $($env:COMPUTERNAME)"
}

if (-not (Test-Path -LiteralPath $runnerRoot -PathType Container)) {
    throw "Authorized runner root does not exist: $runnerRoot"
}

$manifestPath = Join-Path $PackageRoot 'manifest.json'
$payloadPath = Join-Path $PackageRoot 'payload'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Package manifest is missing: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$version = [string]$manifest.version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Package manifest does not contain a version.'
}

foreach ($required in @('MRC.exe', 'MRC.Gui.exe')) {
    $requiredPath = Join-Path $payloadPath $required
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Package payload is missing required file: $required"
    }
}

$versionsRoot = Join-Path $InstallRoot 'versions'
$versionRoot = Join-Path $versionsRoot $version
$binRoot = Join-Path $InstallRoot 'bin'
$configRoot = Join-Path $InstallRoot 'config'
$currentFile = Join-Path $InstallRoot 'current.version'

New-Item -ItemType Directory -Force -Path $versionsRoot, $binRoot, $configRoot | Out-Null

if (-not (Test-Path -LiteralPath $versionRoot -PathType Container)) {
    $stagingRoot = "$versionRoot.staging-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null
    try {
        Copy-Item -Path (Join-Path $payloadPath '*') -Destination $stagingRoot -Recurse -Force
        Move-Item -LiteralPath $stagingRoot -Destination $versionRoot
    }
    finally {
        if (Test-Path -LiteralPath $stagingRoot) {
            Remove-Item -LiteralPath $stagingRoot -Recurse -Force
        }
    }
}

$currentTemp = "$currentFile.tmp-$([Guid]::NewGuid().ToString('N'))"
Set-Content -LiteralPath $currentTemp -Value $version -Encoding ascii -NoNewline
Move-Item -LiteralPath $currentTemp -Destination $currentFile -Force

$launcherPath = Join-Path $binRoot 'MRC.cmd'
$launcher = @'
@echo off
setlocal
set "MRC_HOME=%LOCALAPPDATA%\MRC"
if not exist "%MRC_HOME%\current.version" (
  echo MRC installation is incomplete: current.version is missing. 1>&2
  exit /b 3
)
set /p MRC_VERSION=<"%MRC_HOME%\current.version"
set "MRC_EXE=%MRC_HOME%\versions\%MRC_VERSION%\MRC.exe"
if not exist "%MRC_EXE%" (
  echo MRC installation is incomplete: %MRC_EXE% is missing. 1>&2
  exit /b 3
)
"%MRC_EXE%" %*
exit /b %ERRORLEVEL%
'@
Set-Content -LiteralPath $launcherPath -Value $launcher -Encoding ascii

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
$pathParts = @($userPath -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$alreadyPresent = $pathParts | Where-Object { $_.TrimEnd('\\') -ieq $binRoot.TrimEnd('\\') }
if (-not $alreadyPresent) {
    $newUserPath = if ([string]::IsNullOrWhiteSpace($userPath)) { $binRoot } else { "$userPath;$binRoot" }
    [Environment]::SetEnvironmentVariable('Path', $newUserPath, 'User')
}

Write-Host "MRC $version installed for the current user."
Write-Host "Install root: $InstallRoot"
Write-Host "PATH command: MRC"
Write-Host 'Open a new terminal before invoking MRC from PATH.'
