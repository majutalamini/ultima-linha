"""Folha de contato: python sheet.py frames/kai out.png clip1 clip2 ..."""
import sys
from PIL import Image
d, out, clips = sys.argv[1], sys.argv[2], sys.argv[3:]
rows = []
for c in clips:
    ims = []
    i = 0
    while True:
        try: ims.append(Image.open(f"{d}/{c}_{i:02d}.png").convert("RGBA"))
        except FileNotFoundError: break
        i += 1
    rows.append(ims)
w, h = 256, 384
W = max(len(r) for r in rows) * w
sheet = Image.new("RGBA", (W, h * len(rows)), (60, 62, 70, 255))
for y, r in enumerate(rows):
    for x, im in enumerate(r):
        sheet.alpha_composite(im, (x * w, y * h))
sheet.convert("RGB").save(out)
