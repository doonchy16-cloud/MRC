[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot 'artifacts\pass3\MRC-PASS3-1180x760.png'
}
else {
    $OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
}

$expectedWidth = 1180
$expectedHeight = 760
$project = Join-Path $repoRoot 'tools\MRC.Pass3.Preview\MRC.Pass3.Preview.csproj'
$outputDir = Split-Path $OutputPath -Parent
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Remove-Item -LiteralPath $OutputPath -Force -ErrorAction SilentlyContinue

Push-Location $repoRoot
try {
    & dotnet run --project $project -c Release -- $OutputPath
    if ($LASTEXITCODE -ne 0) {
        throw "PASS 3 preview renderer failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $OutputPath -PathType Leaf)) {
    throw "PASS 3 PNG was not created: $OutputPath"
}

$file = Get-Item -LiteralPath $OutputPath
if ($file.Length -lt 4096) {
    throw "PASS 3 PNG is unexpectedly small ($($file.Length) bytes)."
}

$signature = [System.IO.File]::ReadAllBytes($OutputPath)[0..7]
$expectedSignature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
for ($i = 0; $i -lt $expectedSignature.Length; $i++) {
    if ($signature[$i] -ne $expectedSignature[$i]) {
        throw "PASS 3 output is not a valid PNG signature."
    }
}

Add-Type -AssemblyName PresentationCore
$stream = [System.IO.File]::OpenRead($OutputPath)
try {
    $decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new(
        $stream,
        [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
        [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $frame = $decoder.Frames[0]
    if ($frame.PixelWidth -ne $expectedWidth -or $frame.PixelHeight -ne $expectedHeight) {
        throw "PASS 3 PNG dimensions are $($frame.PixelWidth)x$($frame.PixelHeight), expected ${expectedWidth}x${expectedHeight}."
    }
}
finally {
    $stream.Dispose()
}

Write-Host "PASS 3 PNG verified: $OutputPath"
Write-Host "PNG dimensions: ${expectedWidth}x${expectedHeight}"
Write-Host "PNG bytes: $($file.Length)"
