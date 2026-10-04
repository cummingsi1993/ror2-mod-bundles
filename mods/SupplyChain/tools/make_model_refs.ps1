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

# ---- bulk_order: wooden crate, 3/4 (front + top + side)
$bmp, $g = New-Canvas
$g.FillPolygon((SB 168 120 62), @( (PT 250 360), (PT 640 360), (PT 760 280), (PT 370 280) ))          # top
$g.FillPolygon((SB 124 84 40), @( (PT 250 360), (PT 640 360), (PT 640 760), (PT 250 760) ))           # front
$g.FillPolygon((SB 96 62 28), @( (PT 640 360), (PT 760 280), (PT 760 680), (PT 640 760) ))            # side
$p = PN 70 44 18 9
$g.DrawPolygon($p, @( (PT 250 360), (PT 640 360), (PT 640 760), (PT 250 760) ))
$g.DrawLine($p, 250, 360, 640, 760); $g.DrawLine($p, 640, 360, 250, 760)                              # X brace front
$g.DrawPolygon($p, @( (PT 640 360), (PT 760 280), (PT 760 680), (PT 640 760) ))
$g.DrawPolygon((PN 70 44 18 7), @( (PT 250 360), (PT 640 360), (PT 760 280), (PT 370 280) ))
$g.DrawLine((PN 70 44 18 7), 370, 320, 720, 320)                                                       # top plank line
$bmp.Save("$out\bulk_order_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- loaded_dice: white die 3/4, pips on three faces
$bmp, $g = New-Canvas
$g.FillPolygon((SB 252 252 248), @( (PT 280 380), (PT 620 380), (PT 620 740), (PT 280 740) ))          # front (5)
$g.FillPolygon((SB 226 226 220), @( (PT 280 380), (PT 620 380), (PT 740 290), (PT 400 290) ))          # top (3)
$g.FillPolygon((SB 198 198 192), @( (PT 620 380), (PT 740 290), (PT 740 650), (PT 620 740) ))          # side (1)
$edge = PN 120 120 130 7
$g.DrawPolygon($edge, @( (PT 280 380), (PT 620 380), (PT 620 740), (PT 280 740) ))
$g.DrawPolygon($edge, @( (PT 280 380), (PT 620 380), (PT 740 290), (PT 400 290) ))
$g.DrawPolygon($edge, @( (PT 620 380), (PT 740 290), (PT 740 650), (PT 620 740) ))
$pip = SB 30 30 42
foreach ($pos in @(@(320,420),@(430,520),@(540,620),@(320,620),@(540,420))) { $g.FillEllipse($pip, $pos[0], $pos[1], 56, 56) }   # front: 5
foreach ($pos in @(@(470,310),@(540,330),@(610,350))) { $g.FillEllipse($pip, $pos[0], $pos[1], 40, 32) }                          # top: 3
$g.FillEllipse($pip, 660, 480, 40, 52)                                                                                            # side: 1
$bmp.Save("$out\loaded_dice_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- standing_order: clipboard 3/4 with paper and metal clip
$bmp, $g = New-Canvas
$g.FillPolygon((SB 142 96 48), @( (PT 300 270), (PT 720 250), (PT 750 760), (PT 330 790) ))            # board
$g.FillPolygon((SB 100 66 30), @( (PT 720 250), (PT 750 760), (PT 766 752), (PT 736 244) ))            # board edge
$g.FillPolygon((SB 250 248 240), @( (PT 336 320), (PT 692 302), (PT 716 720), (PT 360 742) ))          # paper
$lp = PN 170 168 158 6
$g.DrawLine($lp, 372, 400, 660, 386); $g.DrawLine($lp, 378, 470, 668, 456); $g.DrawLine($lp, 384, 540, 676, 526)
$g.DrawLine((PN 60 160 80 12), 392, 640, 560, 630)                                                     # green checkline
$g.FillRectangle((SB 150 152 158), 452, 218, 140, 70)                                                   # clip
$g.FillRectangle((SB 110 112 118), 472, 196, 100, 36)
$bmp.Save("$out\standing_order_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- force_multiplier: gold bullhorn, side view
$bmp, $g = New-Canvas
$horn = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.Rectangle(280,240,520,520)), [System.Drawing.Color]::FromArgb(255,255,215,95), [System.Drawing.Color]::FromArgb(255,175,125,30), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillPolygon($horn, @( (PT 430 470), (PT 760 300), (PT 760 720), (PT 430 560) ))                     # cone
$g.FillEllipse($horn, 700, 290, 120, 440)                                                               # bell rim
$g.FillEllipse((SB 120 84 22), 724, 330, 72, 360)                                                       # bell interior
$g.FillRectangle($horn, 310, 460, 130, 110)                                                             # body
$g.FillEllipse($horn, 270, 460, 90, 110)                                                                # cap
$g.FillRectangle((SB 110 76 20), 330, 570, 36, 130)                                                     # grip
$g.FillRectangle((SB 60 40 12), 322, 690, 56, 26)
$g.DrawPolygon((PN 120 84 24 7), @( (PT 430 470), (PT 760 300), (PT 760 720), (PT 430 560) ))
$bmp.Save("$out\force_multiplier_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- pyramid_scheme: gold pyramid with floating capstone, 3/4
$bmp, $g = New-Canvas
$g.FillPolygon((SB 255 220 105), @( (PT 512 330), (PT 812 760), (PT 512 820) ))                         # lit face
$g.FillPolygon((SB 190 140 45), @( (PT 512 330), (PT 512 820), (PT 222 740) ))                          # shaded face
$pen = PN 130 92 25 7
$g.DrawPolygon($pen, @( (PT 512 330), (PT 812 760), (PT 512 820), (PT 222 740) ))
$g.FillPolygon((SB 255 240 160), @( (PT 512 170), (PT 600 290), (PT 512 312) ))                         # capstone lit
$g.FillPolygon((SB 210 165 60), @( (PT 512 170), (PT 512 312), (PT 430 282) ))                          # capstone shaded
$g.DrawLine((PN 130 92 25 5), 512, 318, 512, 326)
$bmp.Save("$out\pyramid_scheme_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- recall_notice: void-purple envelope 3/4, letter out, pale seal
$bmp, $g = New-Canvas
$g.TranslateTransform(512, 350); $g.RotateTransform(-4)
$g.FillRectangle((SB 230 222 240), -200, -150, 400, 220)                                                # letter
$g.DrawRectangle((PN 150 140 165 4), -200, -150, 400, 220)
$lp2 = PN 165 155 180 5
$g.DrawLine($lp2, -170, -110, 170, -110); $g.DrawLine($lp2, -170, -75, 170, -75); $g.DrawLine($lp2, -170, -40, 60, -40)
$g.ResetTransform()
$front = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.Rectangle(210,420,610,330)), [System.Drawing.Color]::FromArgb(255,96,40,130), [System.Drawing.Color]::FromArgb(255,52,18,76), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillPolygon($front, @( (PT 232 460), (PT 792 430), (PT 812 700), (PT 252 730) ))                     # envelope front
$g.FillPolygon((SB 38 12 56), @( (PT 252 730), (PT 812 700), (PT 816 722), (PT 256 752) ))              # bottom edge
$fp = PN 170 120 210 6
$g.DrawLine($fp, 236, 462, 520, 590); $g.DrawLine($fp, 790, 432, 520, 590)                              # flap V
$g.FillEllipse((SB 220 190 245), 472, 548, 96, 96)                                                       # pale seal
$font = New-Object System.Drawing.Font('Georgia', 48, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sf = New-Object System.Drawing.StringFormat; $sf.Alignment='Center'; $sf.LineAlignment='Center'
$g.DrawString('R', $font, (SB 70 28 100), (New-Object System.Drawing.RectangleF(472,548,96,96)), $sf)
$bmp.Save("$out\recall_notice_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- purchase_order: order-form sheet, 3/4 tilt, green check + a pen across it
$bmp, $g = New-Canvas
$g.FillPolygon((SB 210 208 198), @( (PT 300 250), (PT 720 270), (PT 700 800), (PT 280 780) ))           # back/shadow sheet
$g.FillPolygon((SB 248 246 238), @( (PT 280 230), (PT 700 250), (PT 680 778), (PT 260 758) ))           # top sheet
$g.DrawPolygon((PN 150 148 140 4), @( (PT 280 230), (PT 700 250), (PT 680 778), (PT 260 758) ))
$g.FillPolygon((SB 90 200 110), @( (PT 280 230), (PT 700 250), (PT 696 322), (PT 277 302) ))            # green header band
$lp = PN 150 148 140 6
$g.DrawLine($lp, 300, 380, 650, 396); $g.DrawLine($lp, 300, 440, 650, 456); $g.DrawLine($lp, 300, 500, 560, 514); $g.DrawLine($lp, 300, 560, 640, 576)
$a = PN 40 170 70 22
$g.DrawLine($a, 330, 620, 420, 710); $g.DrawLine($a, 420, 710, 640, 470)                                # green approval check
$g.FillPolygon((SB 40 60 130), @( (PT 470 760), (PT 740 300), (PT 770 320), (PT 500 780) ))             # pen barrel
$g.FillPolygon((SB 220 200 90), @( (PT 740 300), (PT 770 320), (PT 786 286) ))                          # nib
$g.FillPolygon((SB 30 40 90), @( (PT 470 760), (PT 500 780), (PT 486 800) ))                            # cap end
$bmp.Save("$out\purchase_order_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---- dropshipping: cardboard parcel 3/4 with a shipping label and a blue down-arrow
$bmp, $g = New-Canvas
$g.FillPolygon((SB 196 150 96), @( (PT 270 380), (PT 660 380), (PT 780 300), (PT 390 300) ))           # top
$g.FillPolygon((SB 168 122 70), @( (PT 270 380), (PT 660 380), (PT 660 760), (PT 270 760) ))           # front
$g.FillPolygon((SB 132 92 50), @( (PT 660 380), (PT 780 300), (PT 780 680), (PT 660 760) ))            # side
$p = PN 96 62 28 7
$g.DrawPolygon($p, @( (PT 270 380), (PT 660 380), (PT 660 760), (PT 270 760) ))
$g.DrawPolygon($p, @( (PT 660 380), (PT 780 300), (PT 780 680), (PT 660 760) ))
$g.DrawPolygon((PN 96 62 28 6), @( (PT 270 380), (PT 660 380), (PT 780 300), (PT 390 300) ))
$g.DrawLine($p, 465, 380, 465, 760)                                                                     # tape seam (front)
$g.DrawLine((PN 210 200 180 6), 270, 380, 660, 380)                                                     # tape (top edge)
$g.FillRectangle((SB 245 245 238), 330, 470, 150, 110)                                                  # shipping label
$g.DrawRectangle((PN 150 150 145 4), 330, 470, 150, 110)
$g.DrawLine((PN 90 90 95 5), 350, 505, 460, 505); $g.DrawLine((PN 90 90 95 5), 350, 530, 460, 530); $g.DrawLine((PN 90 90 95 5), 350, 555, 420, 555)
$a = PN 70 130 235 16                                                                                   # blue delivery down-arrow above box
$g.DrawLine($a, 465, 150, 465, 270); $g.DrawLine($a, 425, 232, 465, 272); $g.DrawLine($a, 505, 232, 465, 272)
$bmp.Save("$out\dropshipping_ref.png"); $g.Dispose(); $bmp.Dispose()

Write-Host "supplychain refs written to $out"
