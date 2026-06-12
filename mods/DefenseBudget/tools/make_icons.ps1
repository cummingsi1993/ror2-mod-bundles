# Generates the mod's icons:
#  - DefenseBudget\assets\icon_item.rgba  (128x128 RGBA32, bottom-up rows, for Texture2D.LoadRawTextureData)
#  - DefenseBudget\assets\icon_buff.rgba  (128x128 RGBA32, white glyph on transparent; tinted by BuffDef.buffColor)
#  - Thunderstore\icon.png                (256x256 PNG, required by Thunderstore)
Add-Type -AssemblyName System.Drawing

$root = Split-Path $PSScriptRoot -Parent

function New-Canvas([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    return $bmp, $g
}

function Draw-ItemIcon([System.Drawing.Graphics]$g, [int]$s) {
    # dark lunar-blue disc with pale blue ring and a gold dollar sign
    $m = [int]($s * 0.04)
    $bg   = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 14, 22, 48))
    $ring = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 130, 180, 255)), ([float]($s * 0.045))
    $g.FillEllipse($bg, $m, $m, $s - 2*$m, $s - 2*$m)
    $g.DrawEllipse($ring, $m, $m, $s - 2*$m, $s - 2*$m)
    # faint 'rising cost' bars behind the glyph
    $bars = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 130, 180, 255))
    $bw = [int]($s * 0.09)
    $g.FillRectangle($bars, [int]($s*0.24), [int]($s*0.58), $bw, [int]($s*0.16))
    $g.FillRectangle($bars, [int]($s*0.38), [int]($s*0.48), $bw, [int]($s*0.26))
    $g.FillRectangle($bars, [int]($s*0.52), [int]($s*0.38), $bw, [int]($s*0.36))
    $g.FillRectangle($bars, [int]($s*0.66), [int]($s*0.28), $bw, [int]($s*0.46))
    $font = New-Object System.Drawing.Font('Arial', ([float]($s * 0.52)), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
    $outline = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 60, 40, 0))
    $gold    = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 200, 60))
    $rect = New-Object System.Drawing.RectangleF(0, 0, $s, $s)
    $o = [float]($s * 0.015)
    foreach ($d in @(@($o,0),@(-$o,0),@(0,$o),@(0,-$o))) {
        $r2 = New-Object System.Drawing.RectangleF($d[0], $d[1], $s, $s)
        $g.DrawString('$', $font, $outline, $r2, $sf)
    }
    $g.DrawString('$', $font, $gold, $rect, $sf)
}

function Draw-BuffIcon([System.Drawing.Graphics]$g, [int]$s) {
    # plain white dollar sign (game tints it via BuffDef.buffColor)
    $font = New-Object System.Drawing.Font('Arial', ([float]($s * 0.78)), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
    $white = [System.Drawing.Brushes]::White
    $rect = New-Object System.Drawing.RectangleF(0, 0, $s, $s)
    $g.DrawString('$', $font, $white, $rect, $sf)
}

function Export-Rgba([System.Drawing.Bitmap]$bmp, [string]$path) {
    $s = $bmp.Width
    $bytes = New-Object byte[] ($s * $s * 4)
    $i = 0
    for ($y = $s - 1; $y -ge 0; $y--) {       # bottom-up for Unity
        for ($x = 0; $x -lt $s; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bytes[$i] = $c.R; $bytes[$i+1] = $c.G; $bytes[$i+2] = $c.B; $bytes[$i+3] = $c.A
            $i += 4
        }
    }
    [System.IO.File]::WriteAllBytes($path, $bytes)
}

# Generic ring icon: tier-colored ring on dark disc, with a per-item glyph drawn by $draw
function New-RingIcon([string]$name, [int[]]$ringRgb, [scriptblock]$draw) {
    $bmp, $g = New-Canvas 128
    $s = 128
    $m = [int]($s * 0.04)
    $bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 18, 18, 26))
    $ring = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, $ringRgb[0], $ringRgb[1], $ringRgb[2])), ([float]($s * 0.045))
    $g.FillEllipse($bg, $m, $m, $s - 2*$m, $s - 2*$m)
    $g.DrawEllipse($ring, $m, $m, $s - 2*$m, $s - 2*$m)
    & $draw $g
    Export-Rgba $bmp (Join-Path $root "DefenseBudget\assets\icon_$name.rgba")
    $g.Dispose(); $bmp.Dispose()
}

function SolidBrush($r,$g2,$b) { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)) }

$sfC = New-Object System.Drawing.StringFormat; $sfC.Alignment='Center'; $sfC.LineAlignment='Center'

# Savings Bond (white ring): cream scroll with red seal
New-RingIcon 'savings_bond' @(235,235,235) {
    param($g)
    $g.FillRectangle((SolidBrush 235 226 192), 26, 48, 76, 34)
    $g.FillEllipse((SolidBrush 222 210 170), 18, 48, 18, 34)
    $g.FillEllipse((SolidBrush 222 210 170), 92, 48, 18, 34)
    $g.FillRectangle((SolidBrush 218 175 70), 54, 44, 20, 42)
    $g.FillEllipse((SolidBrush 178 38 30), 52, 53, 24, 24)
}

# Accounts Receivable (green ring): ledger book with gold $
New-RingIcon 'accounts_receivable' @(120,220,130) {
    param($g)
    $g.FillRectangle((SolidBrush 18 74 46), 36, 32, 58, 64)
    $g.FillRectangle((SolidBrush 12 52 32), 30, 32, 10, 64)
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,218,175,70)), 2.5), 44, 40, 44, 48)
    $f = New-Object System.Drawing.Font('Georgia', 30, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString('$', $f, (SolidBrush 218 175 70), (New-Object System.Drawing.RectangleF(44,40,44,48)), $sfC)
}

# Golden Parachute (red ring): gold canopy + briefcase
New-RingIcon 'golden_parachute' @(235,100,90) {
    param($g)
    $g.FillPie((SolidBrush 235 190 80), 28, 24, 72, 56, 180, 180)
    $p = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,150,108,30)), 3
    $g.DrawLine($p, 32, 52, 56, 86)
    $g.DrawLine($p, 96, 52, 72, 86)
    $g.DrawLine($p, 64, 52, 64, 86)
    $g.FillRectangle((SolidBrush 110 66 30), 48, 86, 32, 24)
    $g.FillRectangle((SolidBrush 235 190 80), 60, 92, 8, 8)
}

# Final Notice (void purple ring): envelope with red stripe and purple seal
New-RingIcon 'final_notice' @(190,120,235) {
    param($g)
    $g.FillRectangle((SolidBrush 240 238 230), 28, 42, 72, 46)
    $p = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,150,145,132)), 2.5
    $g.DrawLine($p, 28, 42, 64, 66)
    $g.DrawLine($p, 100, 42, 64, 66)
    $g.FillRectangle((SolidBrush 200 30 25), 36, 56, 56, 12)
    $g.FillEllipse((SolidBrush 96 40 130), 54, 70, 20, 20)
}

# item icon (128) -> rgba
$bmp, $g = New-Canvas 128
Draw-ItemIcon $g 128
Export-Rgba $bmp (Join-Path $root 'DefenseBudget\assets\icon_item.rgba')
$g.Dispose(); $bmp.Dispose()

# buff icon (128) -> rgba
$bmp, $g = New-Canvas 128
Draw-BuffIcon $g 128
Export-Rgba $bmp (Join-Path $root 'DefenseBudget\assets\icon_buff.rgba')
$g.Dispose(); $bmp.Dispose()

# thunderstore icon (256) -> png
$bmp, $g = New-Canvas 256
$darkbg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 8, 10, 20))
$g.FillRectangle($darkbg, 0, 0, 256, 256)
Draw-ItemIcon $g 256
$bmp.Save((Join-Path $root 'Thunderstore\icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

Write-Host 'icons generated'
