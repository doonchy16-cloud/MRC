$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    dotnet run --project tests/MRC.Redesign.Tests/MRC.Redesign.Tests.csproj -c Release
}
finally {
    Pop-Location
}
