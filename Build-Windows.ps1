$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet SDK not found. Install .NET 8 SDK or newer.'
    }
    dotnet restore .\SparklesReborn.csproj
    dotnet build .\SparklesReborn.csproj -c Debug --no-restore
    Write-Host "`nBuilt: bin\Debug\net8.0-windows\Sparkles.exe" -ForegroundColor Green
}
finally { Pop-Location }
