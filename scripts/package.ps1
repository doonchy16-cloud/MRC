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
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Invalid package version: $version"
}

if ($version -eq '0.1.0') {
    $channel = 'stable'
    $releaseStage = 'Final'
}
elseif ($version -match '^0\.0\.\d+$') {
    $channel = 'precert'
    $releaseStage = 'PreCertification'
}
else {
    throw "Package version $version is outside the authorized MRC release family (0.0.x pre-cert or 0.1.0 final)."
}
$finalTarget = '0.1.0'

# Bootstrap compatibility: installed v0.0.11 hard-validates manifest.channel as "stable"
# before a candidate can activate. Only v0.0.12 uses that legacy transport channel.
# The application itself remains truthfully precert via applicationChannel/releaseStage.
$manifestChannel = if ($version -eq '0.0.12') { 'stable' } else { $channel }

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

$iconMaterializer = Join-Path $repoRoot 'scripts\materialize-icon.ps1'
& $iconMaterializer
$iconPath = Join-Path $repoRoot 'src\MRC.Gui\Assets\MRC.ico'
if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) { throw 'Materialized MRC icon is missing.' }

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

$assemblyVersion = "$version.0"
$publishCommon += "-p:AssemblyVersion=$assemblyVersion"
$publishCommon += "-p:FileVersion=$assemblyVersion"

& dotnet publish (Join-Path $repoRoot 'src\MRC.Cli\MRC.Cli.csproj') @publishCommon
if ($LASTEXITCODE -ne 0) { throw 'CLI publish failed.' }

& dotnet publish (Join-Path $repoRoot 'src\MRC.Gui\MRC.Gui.csproj') @publishCommon
if ($LASTEXITCODE -ne 0) { throw 'GUI publish failed.' }

foreach ($required in @('MRC.exe', 'MRC.Gui.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $payloadRoot $required) -PathType Leaf)) {
        throw "Published payload is missing $required."
    }
}

Copy-Item -LiteralPath $iconPath -Destination (Join-Path $packageRoot 'MRC.ico')
Copy-Item -LiteralPath (Join-Path $repoRoot 'scripts\install.ps1') -Destination (Join-Path $packageRoot 'install.ps1')

$manifest = [ordered]@{
    product = 'Main Runner Control'
    version = $version
    channel = $manifestChannel
    applicationChannel = $channel
    releaseStage = $releaseStage
    finalTarget = $finalTarget
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
Write-Host "Application channel: $channel"
Write-Host "Manifest transport channel: $manifestChannel"
Write-Host "Release stage: $releaseStage"
Write-Host "Final target: $finalTarget"
Write-Host "Candidate package: $zipPath"
Write-Host "Checksum authority: $checksumPath"
