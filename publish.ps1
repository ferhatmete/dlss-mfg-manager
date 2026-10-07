$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'DlssMfgManager\DlssMfgManager.csproj'
$outputPath = Join-Path $PSScriptRoot 'outputs\win-x64-v1.3'

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $outputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded

Write-Host "Yayın hazır: $outputPath\DlssMfgManager.exe"
