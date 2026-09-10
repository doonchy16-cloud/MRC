[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $repoRoot 'artifacts\v0.0.15'
$project = Join-Path $repoRoot 'tools\MRC.Pass3.Preview\MRC.Pass3.Preview.csproj'
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

Add-Type -AssemblyName PresentationCore

Push-Location $repoRoot
try {
    & dotnet build $project -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "v0.0.15 preview project build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

function Assert-PngDimensions {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][int]$Width,
        [Parameter(Mandatory)][int]$Height
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Preview PNG was not created: $Path"
    }

    $file = Get-Item -LiteralPath $Path
    if ($file.Length -lt 4096) {
        throw "Preview PNG is unexpectedly small ($($file.Length) bytes): $Path"
    }

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new(
            $stream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $decoder.Frames[0]
        if ($frame.PixelWidth -ne $Width -or $frame.PixelHeight -ne $Height) {
            throw "Preview PNG dimensions are $($frame.PixelWidth)x$($frame.PixelHeight), expected ${Width}x${Height}: $Path"
        }
    }
    finally {
        $stream.Dispose()
    }

    Write-Host "PREVIEW VERIFIED  ${Width}x${Height}  $Path  $($file.Length) bytes"
}

function Invoke-MrcPreview {
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][int]$Width,
        [Parameter(Mandatory)][int]$Height,
        [string]$FocusState,
        [string]$Drawer,
        [switch]$MixedState
    )

    $outputPath = Join-Path $artifactRoot $FileName
    Remove-Item -LiteralPath $outputPath -Force -ErrorAction SilentlyContinue

    $previewArgs = @(
        '--width', [string]$Width,
        '--height', [string]$Height,
        '--output', $outputPath
    )
    if (-not [string]::IsNullOrWhiteSpace($FocusState)) {
        $previewArgs += @('--focus-state', $FocusState)
    }
    if (-not [string]::IsNullOrWhiteSpace($Drawer)) {
        $previewArgs += @('--drawer', $Drawer)
    }
    if ($MixedState) {
        $previewArgs += '--mixed-state'
    }

    Push-Location $repoRoot
    try {
        & dotnet run --no-build --project $project -c Release -- @previewArgs
        if ($LASTEXITCODE -ne 0) {
            throw "v0.0.15 preview renderer failed with exit code $LASTEXITCODE for ${Width}x${Height}."
        }
    }
    finally {
        Pop-Location
    }

    Assert-PngDimensions -Path $outputPath -Width $Width -Height $Height
}

Invoke-MrcPreview -FileName 'MRC-v0.0.15-2048x1222.png' -Width 2048 -Height 1222
Invoke-MrcPreview -FileName 'MRC-v0.0.15-1200x760.png' -Width 1200 -Height 760
Invoke-MrcPreview -FileName 'MRC-v0.0.15-1180x760.png' -Width 1180 -Height 760
Invoke-MrcPreview -FileName 'MRC-v0.0.15-900x560.png' -Width 900 -Height 560
Invoke-MrcPreview -FileName 'MRC-v0.0.15-900x560-busy.png' -Width 900 -Height 560 -FocusState 'BUSY'
Invoke-MrcPreview -FileName 'MRC-v0.0.15-1200x760-drawer.png' -Width 1200 -Height 760 -Drawer 'control'
Invoke-MrcPreview -FileName 'MRC-v0.0.15-2048x1222-mixed.png' -Width 2048 -Height 1222 -MixedState

Write-Host "V0.0.15 RENDER MATRIX VERIFIED  $artifactRoot"
