[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ReferencePath,
    [string]$CandidatePath,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $repoRoot 'artifacts\v0.0.15'
if ([string]::IsNullOrWhiteSpace($CandidatePath)) {
    $CandidatePath = Join-Path $artifactRoot 'MRC-v0.0.15-2048x1222.png'
}
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = $artifactRoot
}

if (-not (Test-Path -LiteralPath $ReferencePath -PathType Leaf)) {
    throw "Owner reference PNG was not found: $ReferencePath"
}
if (-not (Test-Path -LiteralPath $CandidatePath -PathType Leaf)) {
    throw "Candidate 2048x1222 PNG was not found: $CandidatePath"
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$ReferenceViewportTop = 176
$ComparisonHeight = 884

function Load-Bitmap([string]$Path) {
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new()
    $bitmap.BeginInit()
    $bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $bitmap.UriSource = [Uri]$resolved
    $bitmap.EndInit()
    $bitmap.Freeze()
    return $bitmap
}

function Save-Visual(
    [System.Windows.Media.DrawingVisual]$Visual,
    [int]$Width,
    [int]$Height,
    [string]$Path) {
    $target = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(
        $Width,
        $Height,
        96,
        96,
        [System.Windows.Media.PixelFormats]::Pbgra32)
    $target.Render($Visual)
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($target))
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Create)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}

function Assert-BitmapSize(
    [System.Windows.Media.Imaging.BitmapSource]$Bitmap,
    [int]$Width,
    [int]$Height,
    [string]$Name) {
    if ($Bitmap.PixelWidth -ne $Width -or $Bitmap.PixelHeight -ne $Height) {
        throw "$Name dimensions are $($Bitmap.PixelWidth)x$($Bitmap.PixelHeight), expected ${Width}x${Height}."
    }
}

$reference = Load-Bitmap $ReferencePath
$candidate = Load-Bitmap $CandidatePath
Assert-BitmapSize $reference 2048 1222 'Owner reference'
Assert-BitmapSize $candidate 2048 1222 'Candidate'

$referenceBand = [System.Windows.Media.Imaging.CroppedBitmap]::new(
    $reference,
    [System.Windows.Int32Rect]::new(0, $ReferenceViewportTop, 2048, $ComparisonHeight))
$referenceBand.Freeze()
$candidateBand = [System.Windows.Media.Imaging.CroppedBitmap]::new(
    $candidate,
    [System.Windows.Int32Rect]::new(0, 0, 2048, $ComparisonHeight))
$candidateBand.Freeze()

$sideBySidePath = Join-Path $OutputDir 'reference-side-by-side.png'
$overlayPath = Join-Path $OutputDir 'reference-overlay-50.png'
$neutral = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromRgb(7, 16, 21))
$neutral.Freeze()

$sideBySide = [System.Windows.Media.DrawingVisual]::new()
$dc = $sideBySide.RenderOpen()
try {
    $dc.DrawRectangle($neutral, $null, [System.Windows.Rect]::new(0, 0, 4096, 1222))
    $dc.DrawImage($referenceBand, [System.Windows.Rect]::new(0, 0, 2048, $ComparisonHeight))
    $dc.DrawImage($candidateBand, [System.Windows.Rect]::new(2048, 0, 2048, $ComparisonHeight))
}
finally {
    $dc.Close()
}
Save-Visual $sideBySide 4096 1222 $sideBySidePath

$overlay = [System.Windows.Media.DrawingVisual]::new()
$dc = $overlay.RenderOpen()
try {
    $dc.DrawRectangle($neutral, $null, [System.Windows.Rect]::new(0, 0, 2048, 1222))
    $dc.DrawImage($referenceBand, [System.Windows.Rect]::new(0, 0, 2048, $ComparisonHeight))
    $dc.PushOpacity(0.5)
    $dc.DrawImage($candidateBand, [System.Windows.Rect]::new(0, 0, 2048, $ComparisonHeight))
    $dc.Pop()
}
finally {
    $dc.Close()
}
Save-Visual $overlay 2048 1222 $overlayPath

$side = Load-Bitmap $sideBySidePath
$over = Load-Bitmap $overlayPath
Assert-BitmapSize $side 4096 1222 'Side-by-side comparison'
Assert-BitmapSize $over 2048 1222 '50% overlay comparison'

Write-Host "REFERENCE COMPARISON VERIFIED  $sideBySidePath"
Write-Host "REFERENCE OVERLAY VERIFIED     $overlayPath"
Write-Host "NORMALIZED VIEWPORT BAND       reference y=$ReferenceViewportTop..$($ReferenceViewportTop + $ComparisonHeight), candidate y=0..$ComparisonHeight"
