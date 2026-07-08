#!/usr/bin/env pwsh
# Generates assets/app.ico — a lightning bolt on an indigo->violet rounded square,
# signalling "SmartTyping = fast, smart typing" (the umbrella over language correction
# and text expansion). Multi-resolution (16-256 px). Requires System.Drawing (Windows).
# Also writes assets/app-preview.png.

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root "assets"
$sizes = @(16, 32, 48, 64, 128, 256)
$pngs = @()

function New-RoundedPath([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

$white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'

    # Rounded-square background, diagonal indigo -> violet gradient.
    $rect = New-Object System.Drawing.Rectangle(0, 0, $s, $s)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 99, 91, 255),
        [System.Drawing.Color]::FromArgb(255, 124, 58, 237),
        45)
    $g.FillPath($grad, (New-RoundedPath 0 0 $s $s ($s * 0.22)))

    # Lightning bolt (fractions of $s so it scales to every resolution).
    $bolt = @(
        (New-Object System.Drawing.PointF([single]($s * 0.56), [single]($s * 0.14))),
        (New-Object System.Drawing.PointF([single]($s * 0.30), [single]($s * 0.55))),
        (New-Object System.Drawing.PointF([single]($s * 0.47), [single]($s * 0.55))),
        (New-Object System.Drawing.PointF([single]($s * 0.42), [single]($s * 0.86))),
        (New-Object System.Drawing.PointF([single]($s * 0.70), [single]($s * 0.43))),
        (New-Object System.Drawing.PointF([single]($s * 0.52), [single]($s * 0.43))))
    $g.FillPolygon($white, $bolt)

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , ($ms.ToArray())
    $bmp.Dispose()
}

# Assemble the multi-resolution ICO (PNG-compressed entries).
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $pngs[$i].Length
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([Byte]$dim); $bw.Write([Byte]$dim); $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$len); $bw.Write([UInt32]$offset)
    $offset += $len
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $dir 'app.ico'), $out.ToArray())
[System.IO.File]::WriteAllBytes((Join-Path $dir 'app-preview.png'), $pngs[$sizes.Count - 1])
$bw.Dispose(); $out.Dispose()
Write-Host ("Wrote {0}" -f (Join-Path $dir 'app.ico')) -ForegroundColor Green
