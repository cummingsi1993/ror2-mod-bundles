# Generates HostileWorkplace artifact/buff icons as raw RGBA32 (bottom-up) for
# Texture2D.LoadRawTextureData.
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'HostileWorkplace\assets'
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
function PT($x,$y) { New-Object System.Drawing.Point([int]$x,[int]$y) }

# a dagger pointing up, drawn at an angle via the graphics transform
function Draw-Dagger($g, $cx, $cy, $angle, [bool]$tint) {
    $state = $g.Save()
    $g.TranslateTransform([float]$cx, [float]$cy)
    $g.RotateTransform([float]$angle)
    $blade = if ($tint) { SB 220 225 235 } else { SB 150 150 158 }
    $hilt  = if ($tint) { SB 150 40 40 }  else { SB 90 90 96 }
    $guard = if ($tint) { SB 210 175 70 } else { SB 120 120 126 }
    $g.FillPolygon($blade, @( (PT 0 -46), (PT 7 -10), (PT 0 0), (PT -7 -10) ))   # blade
    $g.FillRectangle($guard, -16, 0, 32, 7)                                       # guard
    $g.FillRectangle($hilt, -4, 7, 8, 26)                                         # grip
    $g.FillEllipse($guard, -6, 31, 12, 12)                                        # pommel
    $g.Restore($state)
}

# Artifact of Mutiny: crossed daggers (enabled = red/steel, disabled = gray)
foreach ($mode in @('enabled','disabled')) {
    $bmp = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $tint = $mode -eq 'enabled'
    $ring = if ($tint) { PN 200 50 50 6 } else { PN 90 90 95 6 }
    $g.DrawEllipse($ring, 8, 8, 112, 112)
    Draw-Dagger $g 64 70 45 $tint
    Draw-Dagger $g 64 70 -45 $tint
    Export-Rgba $bmp (Join-Path $out "icon_artifact_mutiny_$mode.rgba")
    $g.Dispose(); $bmp.Dispose()
}

# Open Season buff: a red target reticle with a dagger through it
$bmp = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.FillEllipse((SB 30 12 12), 6, 6, 116, 116)
$ret = PN 235 60 60 7
$g.DrawEllipse($ret, 24, 24, 80, 80)
$g.DrawEllipse($ret, 44, 44, 40, 40)
$g.DrawLine($ret, 64, 10, 64, 30)
$g.DrawLine($ret, 64, 98, 64, 118)
$g.DrawLine($ret, 10, 64, 30, 64)
$g.DrawLine($ret, 98, 64, 118, 64)
Draw-Dagger $g 64 78 30 $true
Export-Rgba $bmp (Join-Path $out "icon_buff_open_season.rgba")
$g.Dispose(); $bmp.Dispose()

Write-Host "hostileworkplace icons written"
