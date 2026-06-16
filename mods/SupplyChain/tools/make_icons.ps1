# Generates SupplyChain item/artifact icons as raw RGBA32 (bottom-up) for
# Texture2D.LoadRawTextureData.
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'SupplyChain\assets'
New-Item -ItemType Directory -Force $out | Out-Null

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

function SB($r,$g2,$b) { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)) }
function PN($r,$g2,$b,$w) { New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)), ([float]$w) }

function New-RingIcon([string]$name, [int[]]$ring, [scriptblock]$draw) {
    $bmp = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.FillEllipse((SB 18 18 26), 5, 5, 118, 118)
    $g.DrawEllipse((PN $ring[0] $ring[1] $ring[2] 5.5), 5, 5, 118, 118)
    & $draw $g
    Export-Rgba $bmp (Join-Path $out "icon_$name.rgba")
    $g.Dispose(); $bmp.Dispose()
}

# Bulk Order (white): wooden crate
New-RingIcon 'bulk_order' @(235,235,235) {
    param($g)
    $g.FillRectangle((SB 140 95 45), 32, 36, 64, 58)
    $p = PN 95 60 25 5
    $g.DrawRectangle($p, 32, 36, 64, 58)
    $g.DrawLine($p, 32, 36, 96, 94)
    $g.DrawLine($p, 96, 36, 32, 94)
}

# Loaded Dice (green): white die, pips showing 6
New-RingIcon 'loaded_dice' @(120,220,130) {
    param($g)
    $g.FillRectangle((SB 240 240 235), 34, 34, 60, 60)
    $g.DrawRectangle((PN 60 60 70 4), 34, 34, 60, 60)
    $pip = SB 30 30 40
    foreach ($pos in @(@(46,44),@(46,60),@(46,76),@(74,44),@(74,60),@(74,76))) {
        $g.FillEllipse($pip, $pos[0], $pos[1], 10, 10)
    }
}

# Standing Order (red): clipboard with restock arrow
New-RingIcon 'standing_order' @(235,100,90) {
    param($g)
    $g.FillRectangle((SB 150 100 50), 36, 30, 56, 70)
    $g.FillRectangle((SB 240 238 228), 42, 40, 44, 54)
    $g.FillRectangle((SB 120 120 125), 52, 26, 24, 10)
    $p = PN 150 148 140 3
    $g.DrawLine($p, 48, 52, 80, 52)
    $g.DrawLine($p, 48, 62, 80, 62)
    $a = PN 50 160 70 6
    $g.DrawLine($a, 64, 88, 64, 70)
    $g.DrawLine($a, 58, 77, 64, 70)
    $g.DrawLine($a, 70, 77, 64, 70)
}

# Force Multiplier (red): stacked chevrons
New-RingIcon 'force_multiplier' @(235,100,90) {
    param($g)
    $b = SB 235 190 80
    foreach ($y in @(78, 58, 38)) {
        $g.FillPolygon($b, @(
            (New-Object System.Drawing.Point(40, ($y+14))),
            (New-Object System.Drawing.Point(64, $y)),
            (New-Object System.Drawing.Point(88, ($y+14))),
            (New-Object System.Drawing.Point(88, ($y+24))),
            (New-Object System.Drawing.Point(64, ($y+10))),
            (New-Object System.Drawing.Point(40, ($y+24)))
        ))
    }
}

# Pyramid Scheme (lunar blue ring): gold pyramid with floating capstone
New-RingIcon 'pyramid_scheme' @(130,180,255) {
    param($g)
    $g.FillPolygon((SB 218 175 70), @(
        (New-Object System.Drawing.Point(64, 52)),
        (New-Object System.Drawing.Point(96, 96)),
        (New-Object System.Drawing.Point(32, 96))
    ))
    $g.FillPolygon((SB 255 225 110), @(
        (New-Object System.Drawing.Point(64, 28)),
        (New-Object System.Drawing.Point(78, 46)),
        (New-Object System.Drawing.Point(50, 46))
    ))
}

# Purchase Order (green): order form with a green approval check
New-RingIcon 'purchase_order' @(120,220,130) {
    param($g)
    $g.FillRectangle((SB 240 238 228), 34, 28, 60, 74)
    $g.DrawRectangle((PN 120 120 125 3), 34, 28, 60, 74)
    # header band
    $g.FillRectangle((SB 120 220 130), 34, 28, 60, 12)
    # line items
    $p = PN 150 148 140 3
    $g.DrawLine($p, 42, 50, 78, 50)
    $g.DrawLine($p, 42, 60, 78, 60)
    $g.DrawLine($p, 42, 70, 70, 70)
    # big approval checkmark
    $a = PN 40 165 70 7
    $g.DrawLine($a, 46, 84, 58, 96)
    $g.DrawLine($a, 58, 96, 86, 66)
}

# Recall Notice (void purple): envelope with return arrow
New-RingIcon 'recall_notice' @(190,120,235) {
    param($g)
    $g.FillRectangle((SB 96 40 130), 30, 44, 68, 44)
    $p = PN 200 160 230 3.5
    $g.DrawRectangle($p, 30, 44, 68, 44)
    $g.DrawLine($p, 30, 44, 64, 68)
    $g.DrawLine($p, 98, 44, 64, 68)
    $a = PN 240 210 255 5
    $g.DrawArc($a, 44, 70, 40, 34, 300, 220)
    $g.DrawLine($a, 44, 92, 50, 84)
    $g.DrawLine($a, 44, 92, 52, 94)
}

# Artifact of Diversification: four different shapes (enabled gold / disabled gray)
foreach ($mode in @('enabled','disabled')) {
    $bmp = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    if ($mode -eq 'enabled') { $main = SB 255 205 70; $ring = PN 120 220 130 6 } else { $main = SB 130 130 135; $ring = PN 90 90 95 6 }
    $g.DrawEllipse($ring, 8, 8, 112, 112)
    $g.FillEllipse($main, 36, 34, 24, 24)
    $g.FillRectangle($main, 70, 34, 24, 24)
    $g.FillPolygon($main, @(
        (New-Object System.Drawing.Point(48, 68)),
        (New-Object System.Drawing.Point(60, 92)),
        (New-Object System.Drawing.Point(36, 92))
    ))
    $g.FillPolygon($main, @(
        (New-Object System.Drawing.Point(82, 66)),
        (New-Object System.Drawing.Point(94, 80)),
        (New-Object System.Drawing.Point(82, 94)),
        (New-Object System.Drawing.Point(70, 80))
    ))
    Export-Rgba $bmp (Join-Path $out "icon_artifact_diversification_$mode.rgba")
    $g.Dispose(); $bmp.Dispose()
}
Write-Host "supplychain icons written"
