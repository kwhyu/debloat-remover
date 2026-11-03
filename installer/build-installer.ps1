# Builds publish\DebloatManagerSetup.msi and its SHA-256 checksum file.
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\DebloatManager\DebloatManager.csproj"
$publishDir = Join-Path $root "publish\win-x64"
$msiPath = Join-Path $root "publish\DebloatManagerSetup.msi"
$objDir = Join-Path $PSScriptRoot "obj"

function ConvertTo-RtfText([string]$text) {
    $text.Replace('\', '\\').Replace('{', '\{').Replace('}', '\}')
}

function New-EulaRtf([string]$markdownPath, [string]$rtfPath) {
    $body = foreach ($line in Get-Content -Path $markdownPath -Encoding UTF8) {
        $clean = (ConvertTo-RtfText $line).Replace('**', '').Replace('`', '')
        if ($clean -match '^#+ (.*)$') { "\b $($Matches[1])\b0\par" }
        elseif ($clean -match '^- (.*)$') { "\bullet  $($Matches[1])\par" }
        elseif ($clean.Length -eq 0) { "\par" }
        else { "$clean\par" }
    }
    $rtf = "{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\f0\fs18`r`n" + ($body -join "`r`n") + "`r`n}"
    [IO.File]::WriteAllText($rtfPath, $rtf, [Text.Encoding]::ASCII)
}

function New-GrayBitmap([string]$path, [int]$width, [int]$height) {
    Add-Type -AssemblyName System.Drawing
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::FromArgb(240, 240, 240))
    $graphics.Dispose()
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $bitmap.Dispose()
}

[xml]$projectXml = Get-Content -Path $project
$version = $projectXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

Write-Host "Publishing DebloatManager $version..."
dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host "Installing the WiX Toolset CLI..."
    dotnet tool install --global wix --version 5.0.2
    if ($LASTEXITCODE -ne 0) { throw "wix install failed with exit code $LASTEXITCODE" }
}

if ((wix extension list -g) -notmatch "WixToolset.UI.wixext") {
    wix extension add -g WixToolset.UI.wixext/5.0.2
    if ($LASTEXITCODE -ne 0) { throw "wix extension add failed with exit code $LASTEXITCODE" }
}

New-Item -ItemType Directory -Force -Path $objDir | Out-Null
New-EulaRtf (Join-Path $root "EULA.md") (Join-Path $objDir "EULA.rtf")
New-GrayBitmap (Join-Path $objDir "banner.bmp") 493 58
New-GrayBitmap (Join-Path $objDir "dialog.bmp") 493 312

Write-Host "Building the MSI..."
Push-Location $PSScriptRoot
try {
    wix build "Product.wxs" -d "ProductVersion=$version" -ext WixToolset.UI.wixext -arch x64 -o $msiPath
    if ($LASTEXITCODE -ne 0) { throw "wix build failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

$portablePath = Join-Path $root "publish\DebloatManager.exe"
Copy-Item -Path (Join-Path $publishDir "DebloatManager.exe") -Destination $portablePath -Force

foreach ($file in $msiPath, $portablePath) {
    $hash = (Get-FileHash -Path $file -Algorithm SHA256).Hash
    Set-Content -Path "$file.sha256" -Value "$hash  $(Split-Path -Leaf $file)" -Encoding ASCII
    Write-Host "Built: $file"
}
