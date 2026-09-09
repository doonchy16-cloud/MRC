[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'tools\MRC.Pass3.Preview\MRC.Pass3.Preview.csproj'
$outputDir = Join-Path $repoRoot 'artifacts\v0.0.12'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Add-Type -AssemblyName PresentationCore

function Invoke-MrcPreview {
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][int]$Width,
        [Parameter(Mandatory)][int]$Height
    )

    $outputPath = Join-Path $outputDir $FileName
    Remove-Item -LiteralPath $outputPath -Force -ErrorAction SilentlyContinue

    Push-Location $repoRoot
    try {
        & dotnet run --project $project -c Release -- $outputPath $Width $Height
        if ($LASTEXITCODE -ne 0) {
            throw "v0.0.12 preview renderer failed with exit code $LASTEXITCODE for ${Width}x${Height}."
        }
    }
    finally {
        Pop-Location
    }

    if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
        throw "v0.0.12 PNG was not created: $outputPath"
    }

    $file = Get-Item -LiteralPath $outputPath
    if ($file.Length -lt 4096) {
        throw "v0.0.12 PNG is unexpectedly small ($($file.Length) bytes): $outputPath"
    }

    $bytes = [System.IO.File]::ReadAllBytes($outputPath)
    $signature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
    for ($i = 0; $i -lt $signature.Length; $i++) {
        if ($bytes[$i] -ne $signature[$i]) {
            throw "v0.0.12 output is not a valid PNG: $outputPath"
        }
    }

    $stream = [System.IO.File]::OpenRead($outputPath)
    try {
        $decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new(
            $stream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $decoder.Frames[0]
        if ($frame.PixelWidth -ne $Width -or $frame.PixelHeight -ne $Height) {
            throw "v0.0.12 PNG dimensions are $($frame.PixelWidth)x$($frame.PixelHeight), expected ${Width}x${Height}: $outputPath"
        }
    }
    finally {
        $stream.Dispose()
    }

    Write-Host "PREVIEW VERIFIED  ${Width}x${Height}  $outputPath"
}

Invoke-MrcPreview -FileName 'MRC-v0.0.12-1180x760.png' -Width 1180 -Height 760
Invoke-MrcPreview -FileName 'MRC-v0.0.12-900x560.png' -Width 900 -Height 560
