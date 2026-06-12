# 256x256 Thunderstore package icon: crate with amplification chevrons.
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$bmp = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([System.Drawing.Color]::FromArgb(255, 8, 10, 20))
function SB($r,$g2,$b) { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)) }
function PT($x,$y) { New-Object System.Drawing.Point($x,$y) }
$g.FillEllipse((SB 14 22 48), 10, 10, 236, 236)
$g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,130,180,255)), 10), 12, 12, 232, 232)
# crate
$g.FillRectangle((SB 140 95 45), 76, 120, 104, 84)
$p = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,70,44,18)), 7
$g.DrawRectangle($p, 76, 120, 104, 84)
$g.DrawLine($p, 76, 120, 180, 204); $g.DrawLine($p, 180, 120, 76, 204)
# chevrons rising
$b = SB 255 205 70
foreach ($y in @(96, 70, 44)) {
    $g.FillPolygon($b, @( (PT 92 ($y+16)), (PT 128 $y), (PT 164 ($y+16)), (PT 164 ($y+26)), (PT 128 ($y+10)), (PT 92 ($y+26)) ))
}
$bmp.Save((Join-Path $root 'Thunderstore\icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Host "package icon written"
