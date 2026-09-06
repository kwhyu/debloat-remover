Add-Type -AssemblyName System.Drawing

function New-IconFrame {
    param([int]$Size)

    $bmp = [System.Drawing.Bitmap]::new($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    [int]$margin = [Math]::Max(1, [int]($Size * 0.04))
    [int]$rectX = $margin
    [int]$rectY = $margin
    [int]$rectW = $Size - (2 * $margin)
    [int]$rectH = $Size - (2 * $margin)
    $rect = [System.Drawing.Rectangle]::new($rectX, $rectY, $rectW, $rectH)
    [int]$radius = [int]($Size * 0.22)
    [int]$d = $radius * 2

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $colorStart = [System.Drawing.Color]::FromArgb(255, 41, 121, 255)
    $colorEnd = [System.Drawing.Color]::FromArgb(255, 20, 184, 166)
    $brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($rect, $colorStart, $colorEnd, [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    $g.FillPath($brush, $path)

    [single]$cx = $Size / 2.0
    [single]$top = $Size * 0.20
    [single]$bottom = $Size * 0.82
    [single]$halfW = $Size * 0.24

    $shield = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $pt1 = [System.Drawing.PointF]::new($cx, $top)
    $pt2 = [System.Drawing.PointF]::new(($cx + $halfW), ($top + $Size * 0.08))
    $pt3 = [System.Drawing.PointF]::new(($cx + $halfW), ([single]($Size * 0.52)))
    $pt4 = [System.Drawing.PointF]::new($cx, $bottom)
    $pt5 = [System.Drawing.PointF]::new(($cx - $halfW), ([single]($Size * 0.52)))
    $pt6 = [System.Drawing.PointF]::new(($cx - $halfW), ($top + $Size * 0.08))
    $points = [System.Drawing.PointF[]]@($pt1, $pt2, $pt3, $pt4, $pt5, $pt6)
    $shield.AddPolygon($points)
    $g.FillPath([System.Drawing.Brushes]::White, $shield)

    [single]$penWidth = [Math]::Max(2, $Size * 0.07)
    $checkColor = [System.Drawing.Color]::FromArgb(255, 20, 100, 210)
    $pen = [System.Drawing.Pen]::new($checkColor, $penWidth)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $c1 = [System.Drawing.PointF]::new(($cx - $Size * 0.13), ([single]($Size * 0.47)))
    $c2 = [System.Drawing.PointF]::new(($cx - $Size * 0.03), ([single]($Size * 0.58)))
    $c3 = [System.Drawing.PointF]::new(($cx + $Size * 0.16), ([single]($Size * 0.33)))
    $checkPoints = [System.Drawing.PointF[]]@($c1, $c2, $c3)
    $g.DrawLines($pen, $checkPoints)

    $g.Dispose()
    return $bmp
}

$root = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $root "src\DebloatManager\Assets\app.ico"
$previewPath = Join-Path $root "src\DebloatManager\Assets\app-preview.png"

$bmp256 = New-IconFrame -Size 256
$bmp256.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)

$hIcon = $bmp256.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)

$fs = [System.IO.FileStream]::new($outputPath, [System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Flush()
$fs.Close()

$icon.Dispose()
$bmp256.Dispose()

Write-Host "Icon written to $outputPath"
Write-Host "Preview PNG written to $previewPath"
