$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet SDK not found. Install .NET 8 SDK or newer.'
    }
    dotnet restore .\SparklesReborn.csproj
    dotnet publish .\SparklesReborn.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --no-restore
    Write-Host "`nPublished under bin\Release\net8.0-windows\win-x64\publish" -ForegroundColor Green
}
finally { Pop-Location }
