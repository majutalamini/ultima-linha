from PIL import Image, ImageFilter
import numpy as np
P = np.asarray(Image.open('pano_raw.png')).astype(np.float32)
H, W = P.shape[:2]
EXT = 160
print('top rows mean', P[:30].mean(axis=(1,2)).round(1))
# extension: mirror the first EXT rows upward, blur, and fade to near-black
band = P[:EXT][::-1]
band = np.asarray(Image.fromarray(band.astype(np.uint8)).filter(ImageFilter.GaussianBlur(14))).astype(np.float32)
fade = (np.linspace(0.0, 0.45, EXT)**1.6)[:, None, None]   # top -> bottom of extension
base = np.array([2, 2, 2.5], np.float32)
ext = base*(1-fade) + band*fade*0.6
out = np.concatenate([ext, P], axis=0)
# terceiro trecho do vagao um pouco mais escuro (luz falhando)
dim = np.ones(out.shape[1]); x0, x1 = 2900, 3100
dim[x0:x1] = np.linspace(1, 0.78, x1-x0); dim[x1:] = 0.78
out = out*dim[None,:,None]
# right end: 80px fade to black
END = 80
blk = np.zeros((out.shape[0], END, 3), np.float32) + base
out = np.concatenate([out, blk], axis=1)
w = out.shape[1]
ramp = np.ones(w); ramp[W-60:W] = np.linspace(1, 0.35, 60); ramp[W:] = 0.35*np.linspace(1,0,END)
out = out*ramp[None,:,None] + base*(1-ramp[None,:,None])
# left end: the foreground panel already frames it; gentle darkening of first 120px
lr = np.ones(w); lr[:120] = np.linspace(0.55, 1, 120)
out = out*lr[None,:,None] + base*(1-lr[None,:,None])
Image.fromarray(np.clip(out,0,255).astype(np.uint8)).save('cenario1_fundo.png', optimize=True)
print(out.shape)
