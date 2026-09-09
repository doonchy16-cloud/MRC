$ErrorActionPreference = 'Stop'

Write-Host '=== MRC PASS 4 Operations + Updating verification ==='
dotnet run --project ./tests/MRC.Pass4.Tests/MRC.Pass4.Tests.csproj --configuration Release
if ($LASTEXITCODE -ne 0) {
    throw "PASS 4 verification failed with exit code $LASTEXITCODE."
}
