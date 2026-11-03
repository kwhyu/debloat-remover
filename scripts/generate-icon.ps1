# Regenerates the flat gray application icon as a multi-resolution .ico file.
Add-Type -AssemblyName System.Drawing

function New-IconPng([int]$Size) {
    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $diameter = [Math]::Max(2, [int]($Size * 0.36))
    $edge = $Size - 1
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $diameter, $diameter, 180, 90)
    $path.AddArc($edge - $diameter, 0, $diameter, $diameter, 270, 90)
    $path.AddArc($edge - $diameter, $edge - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc(0, $edge - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    $background = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(58, 58, 58))
    $graphics.FillPath($background, $path)

    $foreground = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(224, 224, 224))
    $left = [single]($Size * 0.24)
    $width = [single]($Size * 0.52)
    $height = [single]([Math]::Max(1, $Size * 0.09))
    foreach ($offset in 0.27, 0.455, 0.64) {
        $graphics.FillRectangle($foreground, $left, [single]($Size * $offset), $width, $height)
    }

    $graphics.Dispose()
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    return , $stream.ToArray()
}

$root = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $root "src\DebloatManager\Assets\app.ico"
$sizes = 16, 24, 32, 48, 64, 128, 256
$frames = foreach ($size in $sizes) { , (New-IconPng $size) }

$stream = [System.IO.File]::Create($outputPath)
$writer = New-Object System.IO.BinaryWriter $stream
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)

$offset = 6 + (16 * $sizes.Count)
for ($index = 0; $index -lt $sizes.Count; $index++) {
    $dimension = if ($sizes[$index] -ge 256) { 0 } else { $sizes[$index] }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$frames[$index].Length)
    $writer.Write([uint32]$offset)
    $offset += $frames[$index].Length
}

foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
$writer.Dispose()
$stream.Dispose()

Write-Host "Icon written to $outputPath"
