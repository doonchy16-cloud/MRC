[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$ArtifactsRoot = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts'),
    [string]$VersionOverride = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
[xml]$buildProps = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props') -Raw
$authorityVersion = [string]$buildProps.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($authorityVersion)) {
    throw 'Directory.Build.props does not define Version.'
}

$version = if ([string]::IsNullOrWhiteSpace($VersionOverride)) { $authorityVersion } else { $VersionOverride.Trim() }
if ($version -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') {
    throw "Invalid package version: $version"
}

$packageName = "MRC-v$version-$Runtime"
$stagingRoot = Join-Path $ArtifactsRoot 'staging'
$packageRoot = Join-Path $stagingRoot $packageName
$payloadRoot = Join-Path $packageRoot 'payload'
$zipPath = Join-Path $ArtifactsRoot "$packageName.zip"
$checksumPath = Join-Path $ArtifactsRoot 'SHA256SUMS.txt'

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $payloadRoot | Out-Null
New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null

$publishCommon = @(
    '-c', $Configuration,
    '-r', $Runtime,
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    "-p:Version=$version",
    "-p:InformationalVersion=$version",
    '-o', $payloadRoot
)

if ($version -match '^(\d+)\.(\d+)\.(\d+)$') {
    $assemblyVersion = "$($Matches[1]).$($Matches[2]).$($Matches[3]).0"
    $publishCommon += "-p:AssemblyVersion=$assemblyVersion"
    $publishCommon += "-p:FileVersion=$assemblyVersion"
}

& dotnet publish (Join-Path $repoRoot 'src\MRC.Cli\MRC.Cli.csproj') @publishCommon
if ($LASTEXITCODE -ne 0) { throw 'CLI publish failed.' }

& dotnet publish (Join-Path $repoRoot 'src\MRC.Gui\MRC.Gui.csproj') @publishCommon
if ($LASTEXITCODE -ne 0) { throw 'GUI publish failed.' }

foreach ($required in @('MRC.exe', 'MRC.Gui.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $payloadRoot $required) -PathType Leaf)) {
        throw "Published payload is missing $required."
    }
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'scripts\install.ps1') -Destination (Join-Path $packageRoot 'install.ps1')

$manifest = [ordered]@{
    product = 'Main Runner Control'
    version = $version
    channel = 'stable'
    runtime = $Runtime
    targetMachine = 'DOONCHYSCOMPUTI'
    runnerRoot = 'D:\Git_Runners_Main'
    canonicalCommand = 'MRC'
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding utf8

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumPath -Value "$hash  $([IO.Path]::GetFileName($zipPath))" -Encoding ascii

Remove-Item -LiteralPath $stagingRoot -Recurse -Force

Write-Host "Source authority version: $authorityVersion"
Write-Host "Packaged version: $version"
Write-Host "Candidate package: $zipPath"
Write-Host "Checksum authority: $checksumPath"
