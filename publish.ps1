$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'DlssMfgManager\DlssMfgManager.csproj'
$outputPath = Join-Path $PSScriptRoot 'outputs\win-x64-v1.9.2'

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $outputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish başarısız oldu (çıkış kodu: $LASTEXITCODE)."
}

Write-Host "Yayın hazır: $outputPath\DlssMfgManager.exe"
