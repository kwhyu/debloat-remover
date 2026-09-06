$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "publish\win-x64"
$msiPath = Join-Path $root "publish\DebloatManagerSetup.msi"

Write-Host "Publishing self-contained release build..."
dotnet publish (Join-Path $root "src\DebloatManager\DebloatManager.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host "Installing WiX Toolset CLI (dotnet tool)..."
    dotnet tool install --global wix --version 5.0.2
}

$extensions = wix extension list -g
if ($extensions -notmatch "WixToolset.UI.wixext") {
    Write-Host "Adding WiX UI extension..."
    wix extension add -g WixToolset.UI.wixext/5.0.2
}

Write-Host "Building MSI installer..."
Push-Location $PSScriptRoot
try {
    wix build "Product.wxs" -ext WixToolset.UI.wixext -arch x64 -o $msiPath
    if ($LASTEXITCODE -ne 0) { throw "wix build failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

Write-Host "Installer created at $msiPath"
