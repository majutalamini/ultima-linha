"""Letras escritas em vermelho escuro (tinta/sangue) para o puzzle da palavra secreta, + sons do puzzle."""
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from sons import write, lowpass, metal_hit, t, SR, rng
OUT = 'out/Art/Cenario1'
FONT = '/tmp/claude-0/fonts/ReenieBeanie.ttf'

def letra(ch, seed):
    r = np.random.default_rng(seed)
    S = 3; W, H = 170 * S, 220 * S
    m = Image.new('L', (W, H), 0); d = ImageDraw.Draw(m)
    f = ImageFont.truetype(FONT, 190 * S)
    bb = d.textbbox((0, 0), ch, font=f)
    x = (W - (bb[2] - bb[0])) / 2 - bb[0]; y = 20 * S - bb[1]
    # traço engrossado (pincel/dedo): desenha várias vezes deslocado
    for k in range(10):
        d.text((x + r.normal(0, 2.2 * S), y + r.normal(0, 2.2 * S)), ch, font=f, fill=255)
    m = m.filter(ImageFilter.MaxFilter(5))
    # escorridos para baixo a partir de pontos baixos da letra
    a = np.asarray(m).astype(np.float32) / 255
    cols = np.where(a.max(axis=0) > 0.5)[0]
    for _ in range(4):
        cx = int(r.choice(cols))
        rows = np.where(a[:, cx] > 0.5)[0]
        y0 = rows.max(); L = int(r.uniform(18, 60) * S); w = r.uniform(2.0, 4.0) * S
        d.line([(cx, y0), (cx + r.normal(0, 2) * S, y0 + L)], fill=255, width=int(w))
        d.ellipse([cx - w, y0 + L - w, cx + w * 1.1, y0 + L + w * 1.3], fill=255)
    m = m.filter(ImageFilter.GaussianBlur(1.2 * S))
    a = np.asarray(m).astype(np.float32) / 255
    a = np.clip((a - 0.35) * 3, 0, 1)
    # textura irregular da tinta
    n = np.asarray(Image.fromarray((r.random((H // 6, W // 6)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC)).astype(np.float32) / 255
    col = np.zeros((H, W, 4), np.float32)
    col[..., 0] = 120 + 50 * n; col[..., 1] = 14 + 10 * n; col[..., 2] = 12 + 8 * n
    col[..., 3] = a * (0.75 + 0.25 * n) * 255
    img = Image.fromarray(np.clip(col, 0, 255).astype(np.uint8), 'RGBA').resize((W // S, H // S), Image.LANCZOS)
    return img

for i, ch in enumerate('MORTE'):
    letra(ch, 30 + i).save(f'{OUT}/letra_{ch}.png')
# sombra suave para os pés dos personagens
y, x = np.mgrid[0:64, 0:256]
d = np.hypot((x - 127.5) / 128, (y - 31.5) / 32)
a = np.clip(1 - d, 0, 1) ** 1.5
im = np.zeros((64, 256, 4), np.uint8); im[..., 3] = (a * 255).astype(np.uint8)
Image.fromarray(im, 'RGBA').save(f'{OUT}/sombra_pes.png')

# sons: apagão (estalo + queda de energia), letra encontrada (sussurro grave), tecla do painel
def apagao():
    n = int(2.2 * SR); tt = np.arange(n) / SR
    zap = rng.normal(0, 1, n) * np.exp(-tt / 0.05) * 0.9
    hum = np.sin(2 * np.pi * (120 - 80 * np.clip(tt / 1.2, 0, 1)) * tt) * np.exp(-tt / 0.9) * 0.6
    thud = lowpass(rng.normal(0, 1, n), 90) * np.exp(-tt / 0.4) * 6
    return zap + hum + thud
def achou():
    n = int(1.4 * SR); tt = np.arange(n) / SR
    x = np.sin(2 * np.pi * 110 * tt) * 0.4 + np.sin(2 * np.pi * 164.8 * tt) * 0.3 + np.sin(2 * np.pi * 233 * tt + np.sin(tt * 30)) * 0.15
    air = lowpass(rng.normal(0, 1, n), 1200) - lowpass(rng.normal(0, 1, n), 300)
    env = np.minimum(tt / 0.15, 1) * np.exp(-tt / 0.6)
    return (x + air * 0.5) * env
def tecla():
    return metal_hit(0.12, 900, 0.02, (1, 2.4)) * 0.6
write('apagao', apagao(), 0.8)
write('letra_encontrada', achou(), 0.6)
write('tecla_painel', tecla(), 0.5)
print('ok')
