# Локальный CI-скрипт: restore -> rebuild -> test -> format verify
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet restore DumaLens.sln
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet build DumaLens.sln -t:Rebuild --no-restore -clp:NoSummary
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet test DumaLens.sln --no-build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet format DumaLens.sln --verify-no-changes
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet build-server shutdown
}
finally {
    Pop-Location
}
