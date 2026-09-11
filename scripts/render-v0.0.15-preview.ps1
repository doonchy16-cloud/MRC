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

function Assert-CanonicalAmberField {
    param(
        [Parameter(Mandatory)][string]$Path
    )

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new(
            $stream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $source = $decoder.Frames[0]
        $bitmap = [System.Windows.Media.Imaging.FormatConvertedBitmap]::new()
        $bitmap.BeginInit()
        $bitmap.Source = $source
        $bitmap.DestinationFormat = [System.Windows.Media.PixelFormats]::Bgra32
        $bitmap.EndInit()
        $bitmap.Freeze()

        $width = $bitmap.PixelWidth
        $height = $bitmap.PixelHeight
        $stride = $width * 4
        $pixels = New-Object byte[] ($stride * $height)
        $bitmap.CopyPixels($pixels, $stride, 0)

        $xStart = [int][Math]::Floor($width * 0.60)
        $xEnd = [int][Math]::Ceiling($width * 0.98)
        $yStart = [int][Math]::Floor($height * 0.65)
        $yEnd = [int][Math]::Ceiling($height * 0.98)
        $bestRed = 0
        $bestGreen = 0
        $bestBlue = 255
        $bestWarmth = -255
        $accepted = $false

        for ($y = $yStart; $y -lt $yEnd; $y += 2) {
            for ($x = $xStart; $x -lt $xEnd; $x += 2) {
                $index = ($y * $stride) + ($x * 4)
                $blue = [int]$pixels[$index]
                $green = [int]$pixels[$index + 1]
                $red = [int]$pixels[$index + 2]
                $warmth = $red - $blue

                if ($warmth -gt $bestWarmth) {
                    $bestWarmth = $warmth
                    $bestRed = $red
                    $bestGreen = $green
                    $bestBlue = $blue
                }

                if ($red -ge 72 -and $green -ge 40 -and $blue -le 28 -and $warmth -ge 45) {
                    $accepted = $true
                    break
                }
            }
            if ($accepted) { break }
        }

        if (-not $accepted) {
            throw "F-V15-002: canonical lower-right amber field is too weak. Best warm pixel in review region was RGB($bestRed,$bestGreen,$bestBlue), warmth $bestWarmth; expected at least R72 G40 B<=28 and R-B>=45 near the locked RGB(82,46,17) reference anchor."
        }

        Write-Host "AMBER FIELD VERIFIED  canonical lower-right warm field reaches locked reference-strength band"
    }
    finally {
        $stream.Dispose()
    }
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
Assert-CanonicalAmberField -Path (Join-Path $artifactRoot 'MRC-v0.0.15-2048x1222.png')
Invoke-MrcPreview -FileName 'MRC-v0.0.15-1200x760.png' -Width 1200 -Height 760
Invoke-MrcPreview -FileName 'MRC-v0.0.15-1180x760.png' -Width 1180 -Height 760
Invoke-MrcPreview -FileName 'MRC-v0.0.15-900x560.png' -Width 900 -Height 560
Invoke-MrcPreview -FileName 'MRC-v0.0.15-900x560-busy.png' -Width 900 -Height 560 -FocusState 'BUSY'
Invoke-MrcPreview -FileName 'MRC-v0.0.15-1200x760-drawer.png' -Width 1200 -Height 760 -Drawer 'control'
Invoke-MrcPreview -FileName 'MRC-v0.0.15-2048x1222-mixed.png' -Width 2048 -Height 1222 -MixedState

Write-Host "V0.0.15 RENDER MATRIX VERIFIED  $artifactRoot"
