[CmdletBinding()]
param(
    [string]$Base64Path = (Join-Path (Split-Path $PSScriptRoot -Parent) 'src\MRC.Gui\Assets\MRC.ico.b64'),
    [string]$OutputPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'src\MRC.Gui\Assets\MRC.ico')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $Base64Path -PathType Leaf)) {
    throw "MRC icon authority is missing: $Base64Path"
}

$encoded = (Get-Content -LiteralPath $Base64Path -Raw).Trim()
try {
    $bytes = [Convert]::FromBase64String($encoded)
}
catch {
    throw "MRC.ico.b64 is not valid base64: $($_.Exception.Message)"
}

if ($bytes.Length -lt 128 -or $bytes[0] -ne 0 -or $bytes[1] -ne 0 -or $bytes[2] -ne 1 -or $bytes[3] -ne 0) {
    throw 'Decoded MRC icon authority is not a valid ICO payload.'
}

$outputDirectory = Split-Path $OutputPath -Parent
if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

[IO.File]::WriteAllBytes($OutputPath, $bytes)
Write-Host "MRC icon materialized: $OutputPath"
