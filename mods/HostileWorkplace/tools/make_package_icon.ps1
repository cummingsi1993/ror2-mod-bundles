# 256x256 Thunderstore package icon: ledger with magnifying glass and a vetoed coin.
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$bmp = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([System.Drawing.Color]::FromArgb(255, 8, 10, 20))
function SB($r,$g2,$b) { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)) }
function PN($r,$g2,$b,$w) { New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)), ([float]$w) }
function PT($x,$y) { New-Object System.Drawing.Point($x,$y) }
$g.FillEllipse((SB 14 22 48), 10, 10, 236, 236)
$g.DrawEllipse((PN 124 220 132 10), 12, 12, 232, 232)
# ledger
$g.FillRectangle((SB 240 238 228), 70, 56, 116, 144)
$g.DrawRectangle((PN 120 120 125 6), 70, 56, 116, 144)
$p = PN 150 148 140 6
$g.DrawLine($p, 86, 88, 170, 88)
$g.DrawLine($p, 86, 112, 170, 112)
$g.DrawLine($p, 86, 160, 170, 160)
# the vetoed line
$v = PN 210 40 35 9
$g.DrawLine($v, 80, 136, 176, 136)
# magnifying glass
$m = PN 184 92 224 13
$g.DrawEllipse($m, 118, 110, 72, 72)
$g.DrawLine($m, 180, 172, 220, 212)
# gold coin peeking from behind the ledger
$g.FillEllipse((SB 255 205 70), 34, 150, 56, 56)
$g.DrawEllipse((PN 190 150 40 5), 34, 150, 56, 56)
$bmp.Save((Join-Path $root 'Thunderstore\icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Host "package icon written"
