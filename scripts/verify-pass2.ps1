[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    & dotnet run --project 'tests\MRC.Pass2.Tests\MRC.Pass2.Tests.csproj' -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "PASS 2 verification failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
