# System-python side: build side/face masks from the UV dump and repaint artifacts.
import sys, json, colorsys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
UVJSON, TEX_IN, TEX_OUT, DEBUG = sys.argv[1:5]
N = 512
data = json.load(open(UVJSON))
side = Image.new('L', (N, N), 0); face = Image.new('L', (N, N), 0)
ds, df = ImageDraw.Draw(side), ImageDraw.Draw(face)
for f in data["faces"]:
    poly = [(u * N, (1 - v) * N) for u, v in f["uv"]]  # top-down pixel coords
    if f["d"] < 0.8: ds.polygon(poly, fill=255)
    else: df.polygon(poly, fill=255)
raw = np.frombuffer(open(TEX_IN, 'rb').read(), dtype=np.uint8).reshape(N, N, 4)
tex = raw[::-1].astype(np.float32) / 255.0   # bottom-up -> top-down
rgb = tex[..., :3]
hsv = np.array([colorsys.rgb_to_hsv(*p) for p in rgb.reshape(-1, 3)]).reshape(N, N, 3)
h, s = hsv[..., 0] * 360, hsv[..., 1]
ms = np.array(side.filter(ImageFilter.MaxFilter(3))) > 0
mf = np.array(face) > 0
magenta = ((h > 300) | (h < 12)) & (s > 0.45)
pink = (h > 290) & (h < 350) & (s > 0.3)
gold = (h > 40) & (h < 70) & (s > 0.35)
protect = np.array(Image.fromarray((gold * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(15))) > 0
bad_side = ms & ~mf & ((s < 0.35) | ((h > 190) & (h < 290)))
# salmon/orange smudges sit on the red side of the body pink (~320 deg); gold-pink blends
# land there too, so a band around the gold emblem is protected
bad_face = mf & ~protect & ((h > 340) | (h < 38)) & (s > 0.15)
def fill(bad, good):
    out = rgb.copy(); bad = bad.copy()
    src = np.where(good[..., None], rgb, 0); w = good.astype(np.float32)
    for radius in (4, 10, 24, 60):
        num = np.stack([np.array(Image.fromarray((src[..., c] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius)), np.float32) / 255 for c in range(3)], -1)
        den = np.array(Image.fromarray((w * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius)), np.float32) / 255
        ok = bad & (den > 0.02)
        out[ok] = num[ok] / den[ok][:, None]
        bad &= ~ok
        if not bad.any(): break
    return out
print(f"depth axis {data['axis']}; side texels {int(ms.sum())}, face texels {int(mf.sum())}; repaint side {int(bad_side.sum())}, smudge {int(bad_face.sum())}")
rgb = fill(bad_side, ms & magenta)
# then flatten the whole side band (rim included) to one smooth magenta: TRELLIS's side
# texture is noisy sawtooth, and the band reads best as a solid color. Gold emblem walls keep theirs.
side_all = ms & ~mf & ~protect
base = np.median(rgb[ms & magenta & ~mf], axis=0)
print("side magenta", (base * 255).astype(int))
rgb[side_all] = base
rgb = np.where(bad_face[..., None], fill(bad_face, mf & pink), rgb)
tex[..., :3] = np.clip(rgb, 0, 1)
open(TEX_OUT, 'wb').write((tex[::-1] * 255 + 0.5).astype(np.uint8).tobytes())
dbg = np.zeros((N, N, 3), np.uint8); dbg[ms] = (90, 90, 90); dbg[mf] = (60, 60, 140); dbg[bad_side] = (255, 255, 0); dbg[bad_face] = (255, 120, 0)
Image.fromarray(dbg).resize((512, 512)).save(DEBUG)
Image.fromarray((tex[..., :3] * 255).astype(np.uint8)).save(DEBUG.replace('.png', '_tex.png'))
print("written", TEX_OUT)
