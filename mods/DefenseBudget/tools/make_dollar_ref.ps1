# Reference image for the Defense Budget pickup: a chunky gold dollar sign with visible
# 3D extrusion (drawn as stacked offset glyphs) so TRELLIS reads it as a solid object.
# Also draws the Artifact of Communism lobby icons (enabled/disabled star emblem).
Add-Type -AssemblyName System.Drawing

# ---------- dollar sign ref (1024, white bg)
$bmp = New-Object System.Drawing.Bitmap(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([System.Drawing.Color]::White)

$font = New-Object System.Drawing.Font('Georgia', 620, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sf = New-Object System.Drawing.StringFormat; $sf.Alignment='Center'; $sf.LineAlignment='Center'

# extrusion: stack of dark-gold glyphs receding down-right
for ($i = 26; $i -ge 1; $i--) {
    $shade = [int](95 + $i * 1.5)
    $b = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, $shade, [int]($shade*0.72), 20))
    $rect = New-Object System.Drawing.RectangleF((512 + $i*3 - 40), (488 + $i*2.2), 1024, 1024)
    $rect.X -= 512; $rect.Y -= 512
    $g.DrawString('$', $font, $b, $rect, $sf)
}
# front face: bright gold with vertical gradient
$faceRect = New-Object System.Drawing.Rectangle(0, 0, 1024, 1024)
$face = New-Object System.Drawing.Drawing2D.LinearGradientBrush($faceRect, [System.Drawing.Color]::FromArgb(255,255,225,110), [System.Drawing.Color]::FromArgb(255,200,150,35), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$frontRect = New-Object System.Drawing.RectangleF(-40, -24, 1024, 1024)
$g.DrawString('$', $font, $face, $frontRect, $sf)

$bmp.Save("D:\source\DefenseBudget\mods\DefenseBudget\models\refs\dollar_sign_ref.png")
$g.Dispose(); $bmp.Dispose()

# ---------- artifact icons (128 raw rgba, star emblem; enabled gold / disabled gray)
$root = "D:\source\DefenseBudget\mods\DefenseBudget\DefenseBudget\assets"

function Export-Rgba([System.Drawing.Bitmap]$bmp, [string]$path) {
    $s = $bmp.Width
    $bytes = New-Object byte[] ($s * $s * 4)
    $i = 0
    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $s; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bytes[$i] = $c.R; $bytes[$i+1] = $c.G; $bytes[$i+2] = $c.B; $bytes[$i+3] = $c.A
            $i += 4
        }
    }
    [System.IO.File]::WriteAllBytes($path, $bytes)
}

function Star([int]$cx, [int]$cy, [double]$rOuter, [double]$rInner) {
    $pts = @()
    for ($k = 0; $k -lt 10; $k++) {
        $r = if ($k % 2 -eq 0) { $rOuter } else { $rInner }
        $a = -[math]::PI/2 + $k * [math]::PI/5
        $pts += New-Object System.Drawing.PointF(($cx + $r*[math]::Cos($a)), ($cy + $r*[math]::Sin($a)))
    }
    return $pts
}

foreach ($mode in @('enabled','disabled')) {
    $bmp = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    if ($mode -eq 'enabled') {
        $main = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 205, 70))
        $ring = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 200, 60, 50)), 7.0
    } else {
        $main = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 130, 130, 135))
        $ring = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 90, 90, 95)), 7.0
    }
    $g.DrawEllipse($ring, 8, 8, 112, 112)
    $g.FillPolygon($main, (Star 64 68 44 18))
    Export-Rgba $bmp "$root\icon_artifact_communism_$mode.rgba"
    $g.Dispose(); $bmp.Dispose()
}
Write-Host "dollar ref + artifact icons written"
