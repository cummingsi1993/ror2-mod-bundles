# Redraws the Final Notice reference in 3/4 view with a thin profile and a letter
# poking out, so the 3D generator reads it as a flat envelope rather than a box.
Add-Type -AssemblyName System.Drawing

$bmp = New-Object System.Drawing.Bitmap(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([System.Drawing.Color]::White)

function Brush($r,$g2,$b) { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)) }
function Pen($r,$g2,$b,$w) { New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,$r,$g2,$b)), ([float]$w) }
function Poly($pts) { $pts | ForEach-Object { New-Object System.Drawing.Point($_[0], $_[1]) } }

# letter sticking out of the open top, slightly rotated
$g.TranslateTransform(512, 360)
$g.RotateTransform(-4)
$g.FillRectangle((Brush 252 252 248), -210, -160, 420, 240)
$g.DrawRectangle((Pen 160 158 148 4), -210, -160, 420, 240)
$lp = Pen 175 172 160 5
$g.DrawLine($lp, -180, -120, 180, -120)
$g.DrawLine($lp, -180, -85, 180, -85)
$g.DrawLine($lp, -180, -50, 80, -50)
$g.ResetTransform()

# envelope in 3/4 view: front face is a skewed quad, thin bottom edge visible
# front face (parallelogram leaning right)
$front = Poly @(@(232,470), @(792,430), @(812,710), @(252,750))
$g.FillPolygon((Brush 244 241 232), $front)
$g.DrawPolygon((Pen 120 116 104 5), $front)
# bottom edge (thin depth strip)
$bottom = Poly @(@(252,750), @(812,710), @(816,734), @(256,774))
$g.FillPolygon((Brush 200 196 184), $bottom)
$g.DrawPolygon((Pen 110 106 95 3), $bottom)
# right edge (thin depth strip)
$side = Poly @(@(792,430), @(812,710), @(816,734), @(796,452))
$g.FillPolygon((Brush 214 210 198), $side)
# flap V lines on the front face
$fp = Pen 150 145 132 5
$g.DrawLine($fp, 236, 472, 520, 600)
$g.DrawLine($fp, 790, 432, 520, 600)
# FINAL NOTICE stamp on the front, following the face lean
$g.TranslateTransform(520, 620)
$g.RotateTransform(-5)
$stampPen = Pen 200 30 25 9
$g.DrawRectangle($stampPen, -230, -62, 460, 108)
$fontR = New-Object System.Drawing.Font('Arial Narrow', 58, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sf = New-Object System.Drawing.StringFormat; $sf.Alignment='Center'; $sf.LineAlignment='Center'
$g.DrawString('FINAL NOTICE', $fontR, (Brush 200 30 25), (New-Object System.Drawing.RectangleF(-230,-62,460,108)), $sf)
$g.ResetTransform()
# purple void wax seal at the flap point
$g.FillEllipse((Brush 70 26 96), 472, 558, 96, 96)
$g.FillEllipse((Brush 96 40 130), 482, 568, 76, 76)
$fontS = New-Object System.Drawing.Font('Georgia', 44, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('$', $fontS, (Brush 200 160 230), (New-Object System.Drawing.RectangleF(472,558,96,96)), $sf)

$bmp.Save("D:\source\DefenseBudget\mods\DefenseBudget\models\refs\final_notice_ref.png")
$g.Dispose(); $bmp.Dispose()
Write-Host "final_notice ref v2 written"
