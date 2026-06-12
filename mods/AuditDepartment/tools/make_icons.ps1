# Generates AuditDepartment item/artifact/buff icons as raw RGBA32 (bottom-up) for
# Texture2D.LoadRawTextureData.
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'AuditDepartment\assets'
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

# Red Tape (white): roll of red tape with a trailing strip
New-RingIcon 'red_tape' @(235,235,235) {
    param($g)
    $g.FillPolygon((SB 200 50 45), @(
        (New-Object System.Drawing.Point(58, 60)),
        (New-Object System.Drawing.Point(98, 86)),
        (New-Object System.Drawing.Point(98, 100)),
        (New-Object System.Drawing.Point(58, 74))
    ))
    $g.FillEllipse((SB 225 60 55), 30, 32, 44, 44)
    $g.FillEllipse((SB 18 18 26), 44, 46, 16, 16)
    $g.DrawEllipse((PN 160 35 30 3), 30, 32, 44, 44)
}

# Line-Item Veto (green): document with a red strikethrough over one line
New-RingIcon 'line_item_veto' @(120,220,130) {
    param($g)
    $g.FillRectangle((SB 240 238 228), 38, 28, 52, 72)
    $g.DrawRectangle((PN 120 120 125 3), 38, 28, 52, 72)
    $p = PN 150 148 140 4
    $g.DrawLine($p, 46, 42, 82, 42)
    $g.DrawLine($p, 46, 56, 82, 56)
    $g.DrawLine($p, 46, 84, 82, 84)
    $v = PN 210 40 35 6
    $g.DrawLine($v, 42, 70, 86, 70)
    $g.DrawLine($v, 80, 62, 92, 78)
}

# Hostile Takeover (red): briefcase with an upward breakout arrow
New-RingIcon 'hostile_takeover' @(235,100,90) {
    param($g)
    $g.FillRectangle((SB 90 60 35), 32, 50, 64, 44)
    $g.DrawRectangle((PN 60 40 22 3), 32, 50, 64, 44)
    $g.DrawRectangle((PN 60 40 22 4), 54, 40, 20, 10)
    $g.FillRectangle((SB 218 175 70), 32, 66, 64, 8)
    $a = PN 255 225 110 6
    $g.DrawLine($a, 64, 88, 64, 56)
    $g.DrawLine($a, 54, 66, 64, 56)
    $g.DrawLine($a, 74, 66, 64, 56)
}

# Stimulus Package (lunar blue): parcel with ribbon and a coin burst
New-RingIcon 'stimulus_package' @(130,180,255) {
    param($g)
    $g.FillRectangle((SB 150 105 55), 34, 52, 60, 44)
    $g.DrawRectangle((PN 100 65 30 3), 34, 52, 60, 44)
    $g.FillRectangle((SB 130 180 255), 60, 52, 8, 44)
    $g.FillRectangle((SB 130 180 255), 34, 70, 60, 8)
    $g.FillEllipse((SB 255 215 80), 52, 24, 24, 24)
    $g.DrawEllipse((PN 190 150 40 3), 52, 24, 24, 24)
    $p = PN 255 225 110 3
    $g.DrawLine($p, 44, 30, 36, 22)
    $g.DrawLine($p, 84, 30, 92, 22)
}

# Off the Books (void purple): black ledger with a violet seal and loose page
New-RingIcon 'off_the_books' @(190,120,235) {
    param($g)
    $g.FillRectangle((SB 35 25 48), 34, 30, 56, 68)
    $g.DrawRectangle((PN 96 40 130 4), 34, 30, 56, 68)
    $g.FillRectangle((SB 96 40 130), 34, 30, 10, 68)
    $g.FillEllipse((SB 184 92 224), 56, 52, 22, 22)
    $g.FillPolygon((SB 240 238 228), @(
        (New-Object System.Drawing.Point(78, 84)),
        (New-Object System.Drawing.Point(98, 76)),
        (New-Object System.Drawing.Point(102, 96)),
        (New-Object System.Drawing.Point(82, 104))
    ))
}

# Audited buff: violet magnifying glass over a paper
New-RingIcon 'buff_audited' @(184,92,224) {
    param($g)
    $g.FillRectangle((SB 240 238 228), 44, 36, 40, 52)
    $p = PN 150 148 140 3
    $g.DrawLine($p, 50, 48, 78, 48)
    $g.DrawLine($p, 50, 58, 78, 58)
    $m = PN 184 92 224 7
    $g.DrawEllipse($m, 50, 50, 30, 30)
    $g.DrawLine($m, 76, 76, 92, 92)
}

# Artifact of Austerity: coin being cut by scissors (enabled gold / disabled gray)
foreach ($mode in @('enabled','disabled')) {
    $bmp = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    if ($mode -eq 'enabled') { $coin = SB 255 205 70; $ring = PN 235 100 90 6; $cut = PN 235 235 235 6 } else { $coin = SB 130 130 135; $ring = PN 90 90 95 6; $cut = PN 170 170 175 6 }
    $g.DrawEllipse($ring, 8, 8, 112, 112)
    $g.FillEllipse($coin, 34, 34, 60, 60)
    # the cut: a slash through the coin with a notch
    $g.DrawLine($cut, 28, 92, 100, 36)
    $g.DrawLine($cut, 48, 96, 64, 78)
    Export-Rgba $bmp (Join-Path $out "icon_artifact_austerity_$mode.rgba")
    $g.Dispose(); $bmp.Dispose()
}
Write-Host "auditdepartment icons written"
