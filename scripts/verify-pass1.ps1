[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    & dotnet run --project 'tests\MRC.Tests\MRC.Tests.csproj' -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "PASS 1 verification failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
