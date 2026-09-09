[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'tools\MRC.Pass3.Preview\MRC.Pass3.Preview.csproj'
$outputDir = Join-Path $repoRoot 'artifacts\pass4'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Add-Type -AssemblyName PresentationCore

function Invoke-Pass4Preview {
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
            throw "PASS 4 preview renderer failed with exit code $LASTEXITCODE for ${Width}x${Height}."
        }
    }
    finally {
        Pop-Location
    }

    if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
        throw "PASS 4 PNG was not created: $outputPath"
    }

    $file = Get-Item -LiteralPath $outputPath
    if ($file.Length -lt 4096) {
        throw "PASS 4 PNG is unexpectedly small ($($file.Length) bytes): $outputPath"
    }

    $bytes = [System.IO.File]::ReadAllBytes($outputPath)
    $expectedSignature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
    if ($bytes.Length -lt $expectedSignature.Length) {
        throw "PASS 4 PNG is too small to contain a valid signature: $outputPath"
    }
    for ($i = 0; $i -lt $expectedSignature.Length; $i++) {
        if ($bytes[$i] -ne $expectedSignature[$i]) {
            throw "PASS 4 output is not a valid PNG: $outputPath"
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
            throw "PASS 4 PNG dimensions are $($frame.PixelWidth)x$($frame.PixelHeight), expected ${Width}x${Height}: $outputPath"
        }
    }
    finally {
        $stream.Dispose()
    }

    Write-Host "PASS 4 PNG verified: $outputPath"
    Write-Host "PNG dimensions: ${Width}x${Height}"
    Write-Host "PNG bytes: $($file.Length)"
}

# Authoritative PASS 4 visual geometries: 1180x760 default and 900x560 minimum.
Invoke-Pass4Preview -FileName 'MRC-PASS4-1180x760.png' -Width 1180 -Height 760
Invoke-Pass4Preview -FileName 'MRC-PASS4-900x560.png' -Width 900 -Height 560
