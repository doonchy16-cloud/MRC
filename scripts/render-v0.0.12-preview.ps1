[CmdletBinding()]
param(
    [string]$OutputDir = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\v0.0.12')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'tools\MRC.Pass3.Preview\MRC.Pass3.Preview.csproj'
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Add-Type -AssemblyName PresentationCore

function Invoke-MrcPreview {
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][int]$Width,
        [Parameter(Mandatory)][int]$Height
    )

    $outputPath = Join-Path $OutputDir $FileName
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
        throw "Preview PNG was not created: $outputPath"
    }

    $file = Get-Item -LiteralPath $outputPath
    if ($file.Length -lt 4096) {
        throw "Preview PNG is unexpectedly small ($($file.Length) bytes): $outputPath"
    }

    $stream = [System.IO.File]::OpenRead($outputPath)
    try {
        $decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new(
            $stream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $decoder.Frames[0]
        if ($frame.PixelWidth -ne $Width -or $frame.PixelHeight -ne $Height) {
            throw "Preview PNG dimensions are $($frame.PixelWidth)x$($frame.PixelHeight), expected ${Width}x${Height}: $outputPath"
        }
    }
    finally {
        $stream.Dispose()
    }

    Write-Host "PREVIEW VERIFIED  ${Width}x${Height}  $outputPath  $($file.Length) bytes"
}

Invoke-MrcPreview -FileName 'MRC-v0.0.12-1180x760.png' -Width 1180 -Height 760
Invoke-MrcPreview -FileName 'MRC-v0.0.12-900x560.png' -Width 900 -Height 560
