# Reference image for the Overtime Pay pickup: a glossy void-pink heart with a gold $
# emblem, in 3/4 view with visible extrusion (stacked offset shapes) so TRELLIS reads it
# as a solid object. Bright colors on purpose: TRELLIS bakes dark refs into muddy textures.
# This exact image (seed 42) produced the shipped model. TRELLIS bakes the gloss highlight
# into an orange smudge and grey sawtooth sides; fix_overtime_pay_model.sh repaints both.
# (Dropping the highlight and smoothing the extrusion gave clean sides but a muddy, flat $.)
param([string]$Out = "D:\source\DefenseBudget\mods\DefenseBudget\models\refs\overtime_pay_ref.png")
Add-Type -AssemblyName System.Drawing

function Heart-Path([float]$cx, [float]$cy, [float]$s) {
    # unit heart (same curves as the inventory icon), centered on (cx, cy), width ~ s
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    function P([float]$x, [float]$y) { New-Object System.Drawing.PointF(($cx + ($x - 64) * $s / 78), ($cy + ($y - 64) * $s / 78)) }
    $p.AddBezier((P 64 44), (P 56 26), (P 24 28), (P 26 54))
    $p.AddBezier((P 26 54), (P 28 72), (P 50 86), (P 64 100))
    $p.AddBezier((P 64 100), (P 78 86), (P 100 72), (P 102 54))
    $p.AddBezier((P 102 54), (P 104 28), (P 72 26), (P 64 44))
    $p.CloseFigure()
    return $p
}

$bmp = New-Object System.Drawing.Bitmap(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([System.Drawing.Color]::White)

$cx = 490; $cy = 470; $size = 640

# extrusion: darker magenta hearts receding down-right
for ($i = 30; $i -ge 1; $i--) {
    $shade = [int](120 + $i * 1.6)
    $b = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, $shade, [int]($shade * 0.25), [int]($shade * 0.62)))
    $g.FillPath($b, (Heart-Path ($cx + $i * 2.6) ($cy + $i * 1.8) $size))
}

# front face: vertical pink gradient
$front = Heart-Path $cx $cy $size
$bounds = $front.GetBounds()
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bounds, [System.Drawing.Color]::FromArgb(255, 255, 140, 210), [System.Drawing.Color]::FromArgb(255, 225, 60, 160), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillPath($grad, $front)
$g.DrawPath((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 170, 40, 120)), 6), $front)

# glossy highlight on the upper-left lobe
$hl = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(110, 255, 255, 255))
$g.FillEllipse($hl, ($cx - 225), ($cy - 205), 150, 90)

# gold $ emblem with its own small extrusion
$font = New-Object System.Drawing.Font('Georgia', 330, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sf = New-Object System.Drawing.StringFormat; $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
for ($i = 8; $i -ge 1; $i--) {
    $b = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 170, 115, 20))
    $g.DrawString('$', $font, $b, (New-Object System.Drawing.RectangleF(($cx - 300 + $i * 2), ($cy - 330 + $i * 1.5), 600, 600)), $sf)
}
$goldRect = New-Object System.Drawing.Rectangle(($cx - 150), ($cy - 200), 300, 360)
$gold = New-Object System.Drawing.Drawing2D.LinearGradientBrush($goldRect, [System.Drawing.Color]::FromArgb(255, 255, 230, 120), [System.Drawing.Color]::FromArgb(255, 220, 160, 40), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.DrawString('$', $font, $gold, (New-Object System.Drawing.RectangleF(($cx - 300), ($cy - 330), 600, 600)), $sf)

$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Host "overtime_pay ref written: $Out"
