# Reference images (1024, white bg, 3/4 views with depth cues) for TRELLIS.2.
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'models\refs'
New-Item -ItemType Directory -Force $out | Out-Null

function New-Canvas {
    $bmp = New-Object System.Drawing.Bitmap(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([System.Drawing.Color]::White)
    return $bmp, $g
}
function SB($r,$g2,$b) { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)) }
function PN($r,$g2,$b,$w) { New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)), ([float]$w) }
function PT($x,$y) { New-Object System.Drawing.Point($x,$y) }

# ---- red_tape: red tape roll 3/4 (cylinder with hole), strip trailing off
$bmp, $g = New-Canvas
$g.FillPolygon((SB 178 32 28), @( (PT 380 300), (PT 380 700), (PT 560 740), (PT 560 340) ))            # roll edge (depth)
$g.FillEllipse((SB 225 55 48), 200, 280, 380, 440)                                                      # face
$g.DrawEllipse((PN 150 26 22 9), 200, 280, 380, 440)
$g.FillEllipse((SB 248 246 242), 320, 420, 140, 160)                                                    # core hole
$g.DrawEllipse((PN 150 26 22 7), 320, 420, 140, 160)
$strip = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.Rectangle(540,520,360,260)), [System.Drawing.Color]::FromArgb(255,225,55,48), [System.Drawing.Color]::FromArgb(255,170,30,26), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillPolygon($strip, @( (PT 545 600), (PT 880 540), (PT 900 640), (PT 560 712) ))                     # trailing strip
$g.FillPolygon((SB 150 26 22), @( (PT 880 540), (PT 900 640), (PT 930 600) ))                           # torn end
$bmp.Save("$out\red_tape_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- line_item_veto: rubber stamp standing on a document with a red strike
$bmp, $g = New-Canvas
$g.FillPolygon((SB 250 248 240), @( (PT 180 620), (PT 760 580), (PT 860 800), (PT 280 850) ))           # paper
$g.DrawPolygon((PN 160 158 150 5), @( (PT 180 620), (PT 760 580), (PT 860 800), (PT 280 850) ))
$lp = PN 170 168 158 6
$g.DrawLine($lp, 260, 680, 700, 650); $g.DrawLine($lp, 280, 730, 730, 700); $g.DrawLine($lp, 300, 780, 760, 750)
$g.DrawLine((PN 205 38 32 14), 250, 705, 740, 672)                                                      # red strike
$hb = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.Rectangle(420,150,200,300)), [System.Drawing.Color]::FromArgb(255,185,135,75), [System.Drawing.Color]::FromArgb(255,120,80,40), [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
$g.FillEllipse($hb, 470, 140, 110, 90)                                                                  # knob
$g.FillRectangle($hb, 495, 210, 60, 170)                                                                # handle stem
$g.FillPolygon($hb, @( (PT 460 380), (PT 590 380), (PT 640 460), (PT 410 460) ))                        # base taper
$g.FillPolygon((SB 70 70 78), @( (PT 380 460), (PT 670 440), (PT 700 540), (PT 410 560) ))              # rubber base
$g.FillPolygon((SB 45 45 52), @( (PT 410 560), (PT 700 540), (PT 702 562), (PT 412 582) ))              # base bottom edge
$bmp.Save("$out\line_item_veto_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- hostile_takeover: leather briefcase 3/4 with gold clasps
$bmp, $g = New-Canvas
$g.FillPolygon((SB 96 62 32), @( (PT 240 380), (PT 700 380), (PT 800 320), (PT 340 320) ))              # top
$g.FillPolygon((SB 132 88 46), @( (PT 240 380), (PT 700 380), (PT 700 760), (PT 240 760) ))             # front
$g.FillPolygon((SB 84 54 26), @( (PT 700 380), (PT 800 320), (PT 800 700), (PT 700 760) ))              # side
$p = PN 58 36 16 8
$g.DrawPolygon($p, @( (PT 240 380), (PT 700 380), (PT 700 760), (PT 240 760) ))
$g.DrawPolygon($p, @( (PT 700 380), (PT 800 320), (PT 800 700), (PT 700 760) ))
$g.DrawPolygon((PN 58 36 16 6), @( (PT 240 380), (PT 700 380), (PT 800 320), (PT 340 320) ))
$g.DrawLine($p, 240, 470, 700, 470); $g.DrawLine($p, 700, 470, 800, 410)                                # lid seam
$g.FillRectangle((SB 58 36 16), 420, 268, 120, 60)                                                      # handle
$g.FillRectangle((SB 255 255 255), 444, 288, 72, 40)
$g.FillRectangle((SB 235 188 80), 330, 440, 56, 60)                                                     # clasps
$g.FillRectangle((SB 235 188 80), 560, 440, 56, 60)
$g.DrawRectangle((PN 160 120 35 5), 330, 440, 56, 60)
$g.DrawRectangle((PN 160 120 35 5), 560, 440, 56, 60)
$bmp.Save("$out\hostile_takeover_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- stimulus_package: brown parcel 3/4 with blue ribbon and gold coins
$bmp, $g = New-Canvas
$g.FillPolygon((SB 196 150 96), @( (PT 250 420), (PT 660 420), (PT 780 340), (PT 370 340) ))            # top
$g.FillPolygon((SB 160 116 66), @( (PT 250 420), (PT 660 420), (PT 660 780), (PT 250 780) ))            # front
$g.FillPolygon((SB 128 90 48), @( (PT 660 420), (PT 780 340), (PT 780 700), (PT 660 780) ))             # side
$rib = SB 86 142 230
$g.FillPolygon($rib, @( (PT 430 420), (PT 490 420), (PT 490 780), (PT 430 780) ))                       # ribbon front
$g.FillPolygon($rib, @( (PT 430 420), (PT 490 420), (PT 610 340), (PT 550 340) ))                       # ribbon top
$g.FillPolygon($rib, @( (PT 250 570), (PT 660 570), (PT 780 490), (PT 370 490) ))                       # hmm cross strap on top only
$p2 = PN 96 62 28 7
$g.DrawPolygon($p2, @( (PT 250 420), (PT 660 420), (PT 660 780), (PT 250 780) ))
$g.DrawPolygon($p2, @( (PT 660 420), (PT 780 340), (PT 780 700), (PT 660 780) ))
$g.DrawPolygon((PN 96 62 28 6), @( (PT 250 420), (PT 660 420), (PT 780 340), (PT 370 340) ))
foreach ($c in @(@(620,250),@(700,230),@(660,190))) {                                                    # coins above
    $g.FillEllipse((SB 255 210 80), $c[0], $c[1], 96, 72)
    $g.DrawEllipse((PN 185 145 40 6), $c[0], $c[1], 96, 72)
}
$bmp.Save("$out\stimulus_package_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- off_the_books: closed dark ledger 3/4, violet wax seal, page edges
$bmp, $g = New-Canvas
$g.FillPolygon((SB 42 30 58), @( (PT 240 320), (PT 700 300), (PT 760 360), (PT 300 382) ))              # cover top edge
$g.FillPolygon((SB 56 40 78), @( (PT 240 320), (PT 300 382), (PT 300 800), (PT 240 740) ))              # spine
$g.FillPolygon((SB 64 46 90), @( (PT 300 382), (PT 760 360), (PT 760 778), (PT 300 800) ))              # front cover
$g.DrawPolygon((PN 30 20 44 7), @( (PT 300 382), (PT 760 360), (PT 760 778), (PT 300 800) ))
$g.FillPolygon((SB 238 232 214), @( (PT 760 372), (PT 786 368), (PT 786 760), (PT 760 766) ))           # page block
$lp2 = PN 200 194 175 3
foreach ($y in @(420, 470, 520, 570, 620, 670, 720)) { $g.DrawLine($lp2, 762, $y, 784, ($y - 2)) }
$g.FillRectangle((SB 120 70 165), 380, 430, 300, 56)                                                     # violet band
$g.FillEllipse((SB 150 78 198), 480, 540, 130, 130)                                                      # wax seal
$g.FillEllipse((SB 110 52 150), 505, 565, 80, 80)
$g.DrawEllipse((PN 90 40 124 6), 480, 540, 130, 130)
$bmp.Save("$out\off_the_books_ref.png"); $g.Dispose(); $bmp.Dispose()

Write-Host "auditdepartment refs written to $out"
