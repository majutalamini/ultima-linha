from PIL import Image, ImageDraw
import numpy as np
src = Image.open('ref_a.png').convert('RGB')
A = np.asarray(src).astype(np.float32)
H, W = A.shape[:2]
R, L = 1640, 315          # tile joins: source x=R continues at source x=L
BLEND = 24
period = R - L
def build(ntiles, end_src):
    pieces = [A[:, :R]]
    for k in range(1, ntiles):
        stop = R if k < ntiles-1 else end_src
        pieces.append(A[:, L:stop])
    out = pieces[0]
    for p in pieces[1:]:
        # crossfade: last BLEND cols of out with source columns that continue
        n = BLEND
        tailsrc = A[:, R:R+n] if R+n <= W else None
        # blend region: overlap the first n cols of p with the cols after R from the original
        ov_a = A[:, R:R+n]           # what would follow in tile k-1
        ov_b = p[:, :n]              # tile k start
        w = np.linspace(0, 1, n)[None, :, None]
        mixed = ov_a*(1-w) + ov_b*w
        out = np.concatenate([out, mixed, p[:, n:]], axis=1)
    return out
pano = build(3, 1500)
print(pano.shape)
Image.fromarray(np.clip(pano,0,255).astype(np.uint8)).save('pano_raw.png')
