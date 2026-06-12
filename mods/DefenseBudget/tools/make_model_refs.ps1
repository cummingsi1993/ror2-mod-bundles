# Draws 1024x1024 reference images (pure white background, per the 3d-pipeline doc's
# rigid-item guidance) for TRELLIS.2 image-to-3D generation.
Add-Type -AssemblyName System.Drawing

$out = "D:\source\DefenseBudget\mods\DefenseBudget\models\refs"
New-Item -ItemType Directory -Force $out | Out-Null

function New-Canvas {
    $bmp = New-Object System.Drawing.Bitmap(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([System.Drawing.Color]::White)
    return $bmp, $g
}

function Brush($r,$g2,$b)  { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)) }
function Pen($r,$g2,$b,$w) { New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)), ([float]$w) }
function GradBrush($x,$y,$w,$h,$c1,$c2) {
    $rect = New-Object System.Drawing.Rectangle($x,$y,$w,$h)
    New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
}
function C($r,$g2,$b) { [System.Drawing.Color]::FromArgb(255,$r,$g2,$b) }

# ---------------- 1. Savings Bond: rolled cream scroll, gold band, red wax seal with $
$bmp, $g = New-Canvas
# main scroll body (horizontal cylinder)
$body = GradBrush 212 352 600 320 (C 245 238 210) (C 205 192 150)
$g.FillRectangle($body, 262, 392, 500, 240)
# cylinder end caps (rolled spiral on right end)
$g.FillEllipse((GradBrush 712 392 140 240 (C 235 226 192) (C 188 172 128)), 692, 392, 140, 240)
$g.FillEllipse((Brush 224 213 176), 712, 422, 100, 180)
$g.FillEllipse((Brush 200 186 142), 732, 457, 60, 110)
$g.FillEllipse((Brush 170 154 110), 747, 487, 30, 50)
# left end cap
$g.FillEllipse((GradBrush 192 392 140 240 (C 250 244 222) (C 215 202 162)), 192, 392, 140, 240)
$g.FillEllipse((Brush 238 230 200), 212, 422, 100, 180)
$g.FillEllipse((Brush 215 202 162), 232, 457, 60, 110)
# subtle paper lines along the roll
$linePen = Pen 185 170 128 3
$g.DrawLine($linePen, 280, 440, 700, 440)
$g.DrawLine($linePen, 280, 512, 700, 512)
$g.DrawLine($linePen, 280, 584, 700, 584)
# gold band around the middle
$g.FillRectangle((GradBrush 430 380 130 264 (C 255 215 90) (C 190 145 30)), 430, 384, 130, 256)
$g.DrawRectangle((Pen 150 110 20 6), 430, 384, 130, 256)
# red wax seal on the band
$g.FillEllipse((GradBrush 420 432 160 160 (C 205 50 40) (C 130 20 15)), 420, 432, 160, 160)
$g.FillEllipse((Brush 178 38 30), 438, 450, 124, 124)
$font = New-Object System.Drawing.Font('Georgia', 64, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sf = New-Object System.Drawing.StringFormat; $sf.Alignment='Center'; $sf.LineAlignment='Center'
$g.DrawString('$', $font, (Brush 250 215 160), (New-Object System.Drawing.RectangleF(420,432,160,160)), $sf)
$bmp.Save("$out\savings_bond_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---------------- 2. Accounts Receivable: thick dark-green ledger book, gold trim and $
$bmp, $g = New-Canvas
# page block (visible from the right/bottom edges)
$g.FillRectangle((GradBrush 300 320 480 420 (C 250 247 235) (C 210 204 180)), 320, 340, 460, 400)
$pagePen = Pen 190 184 160 2
for ($i = 0; $i -lt 9; $i++) { $g.DrawLine($pagePen, 784, (352 + $i*42), 784, (382 + $i*42)) }
# front cover, slightly offset to show page depth
$cover = GradBrush 240 280 520 440 (C 30 92 58) (C 12 48 30)
$g.FillRectangle($cover, 244, 284, 520, 440)
$g.DrawRectangle((Pen 8 30 18 8), 244, 284, 520, 440)
# spine on the left
$g.FillRectangle((GradBrush 196 280 70 450 (C 20 64 40) (C 6 28 16)), 196, 284, 70, 440)
# gold corner protectors
$gold = Brush 218 175 70
$g.FillPolygon($gold, @( (New-Object System.Drawing.Point(700,284)), (New-Object System.Drawing.Point(764,284)), (New-Object System.Drawing.Point(764,348)) ))
$g.FillPolygon($gold, @( (New-Object System.Drawing.Point(700,724)), (New-Object System.Drawing.Point(764,724)), (New-Object System.Drawing.Point(764,660)) ))
# gold frame + title area
$g.DrawRectangle((Pen 218 175 70 6), 300, 340, 400, 330)
$font2 = New-Object System.Drawing.Font('Georgia', 150, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('$', $font2, $gold, (New-Object System.Drawing.RectangleF(300,360,400,260)), $sf)
$fontSm = New-Object System.Drawing.Font('Georgia', 36, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('LEDGER', $fontSm, $gold, (New-Object System.Drawing.RectangleF(300,612,400,60)), $sf)
$bmp.Save("$out\accounts_receivable_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---------------- 3. Golden Parachute: gold canopy, straps, brown briefcase below
$bmp, $g = New-Canvas
# canopy (half-dome)
$canopyRect = New-Object System.Drawing.Rectangle(212, 120, 600, 480)
$canopy = New-Object System.Drawing.Drawing2D.LinearGradientBrush($canopyRect, (C 255 215 90), (C 175 125 25), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillPie($canopy, 212, 120, 600, 480, 180, 180)
# canopy gores (vertical seams)
$gorePen = Pen 140 100 20 6
foreach ($x in @(312, 412, 512, 612, 712)) { $g.DrawLine($gorePen, $x, ([int](360 - [math]::Sqrt([math]::Max(0, 1 - [math]::Pow(($x-512)/300.0, 2)) ) * 235)), 512, 380) }
# canopy bottom edge scallops
$g.FillPie((Brush 150 105 22), 212, 330, 600, 60, 0, 180)
# straps from canopy edge down to briefcase
$strapPen = Pen 110 78 18 12
$g.DrawLine($strapPen, 232, 372, 462, 700)
$g.DrawLine($strapPen, 792, 372, 562, 700)
$g.DrawLine($strapPen, 372, 380, 492, 700)
$g.DrawLine($strapPen, 652, 380, 532, 700)
# briefcase
$case = GradBrush 392 690 240 180 (C 130 82 40) (C 78 46 20)
$g.FillRectangle($case, 392, 700, 240, 170)
$g.DrawRectangle((Pen 56 32 12 8), 392, 700, 240, 170)
# briefcase handle + clasps + gold $
$g.DrawArc((Pen 56 32 12 14), 462, 668, 100, 60, 180, 180)
$g.FillRectangle((Brush 218 175 70), 432, 740, 28, 22)
$g.FillRectangle((Brush 218 175 70), 564, 740, 28, 22)
$fontC = New-Object System.Drawing.Font('Arial', 80, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('$', $fontC, (Brush 230 190 90), (New-Object System.Drawing.RectangleF(392,716,240,150)), $sf)
$bmp.Save("$out\golden_parachute_ref.png"); $g.Dispose(); $bmp.Dispose()

# ---------------- 4. Final Notice: white envelope, red FINAL NOTICE stamp, dark purple void wax seal
$bmp, $g = New-Canvas
# envelope body (slightly angled rectangle via offset shadow)
$g.FillRectangle((Brush 215 212 205), 222, 342, 600, 360)   # shadow
$env = GradBrush 200 320 600 360 (C 252 250 244) (C 222 218 205)
$g.FillRectangle($env, 212, 332, 600, 360)
$g.DrawRectangle((Pen 120 115 105 5), 212, 332, 600, 360)
# flap lines (V from top corners to center)
$flapPen = Pen 150 145 132 5
$g.DrawLine($flapPen, 212, 332, 512, 520)
$g.DrawLine($flapPen, 812, 332, 512, 520)
# red FINAL NOTICE stamp (rotated)
$g.TranslateTransform(512, 470)
$g.RotateTransform(-12)
$stampPen = Pen 200 30 25 10
$g.DrawRectangle($stampPen, -250, -75, 500, 130)
$fontR = New-Object System.Drawing.Font('Arial Narrow', 72, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('FINAL NOTICE', $fontR, (Brush 200 30 25), (New-Object System.Drawing.RectangleF(-250,-75,500,130)), $sf)
$g.ResetTransform()
# dark void-purple wax seal, bottom center
$g.FillEllipse((GradBrush 442 560 140 140 (C 96 40 130) (C 40 12 60)), 442, 560, 140, 140)
$g.FillEllipse((Brush 70 26 96), 458, 576, 108, 108)
$fontS = New-Object System.Drawing.Font('Georgia', 56, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('$', $fontS, (Brush 200 160 230), (New-Object System.Drawing.RectangleF(442,560,140,140)), $sf)
$bmp.Save("$out\final_notice_ref.png"); $g.Dispose(); $bmp.Dispose()

Write-Host "refs written to $out"
