"""Diário aberto (duas páginas de papel envelhecido) para a cutscene de abertura."""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
W, H = 2400, 1350
rng = np.random.default_rng(20)

def blur(a, r):
    return np.asarray(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(r))).astype(np.float32)

def noise(scale, seed):
    r = np.random.default_rng(seed)
    small = r.random((H // scale + 2, W // scale + 2)) * 255
    im = Image.fromarray(small.astype(np.uint8)).resize((W + 2 * scale, H + 2 * scale), Image.BICUBIC)
    return (np.asarray(im).astype(np.float32)[:H, :W] / 255.0) - 0.5

def diario():
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    base = np.array([172, 156, 124], np.float32)
    a = np.ones((H, W, 3), np.float32) * base
    a += (noise(220, 1) * 28 + noise(70, 2) * 14 + noise(14, 3) * 6)[..., None]
    a += rng.normal(0, 3.2, (H, W))[..., None]
    fib = blur(rng.random((H, W)) * 255 * (rng.random((H, W)) > 0.997), 0.8)
    a -= (fib / 255 * 30)[..., None]
    cx = W / 2
    # curvatura das folhas: escurece perto da costura e nas bordas externas
    g = np.abs(x - cx) / cx                      # 0 no meio, 1 nas bordas
    gutter = np.exp(-(g / 0.035) ** 1.3) * 0.75 + np.exp(-(g / 0.16) ** 2) * 0.22
    a *= (1 - gutter)[..., None]
    a += (np.exp(-((g - 0.05) / 0.03) ** 2) * 10)[..., None]   # brilho onde a folha "sobe"
    outer = np.clip((g - 0.86) / 0.14, 0, 1) ** 2
    a *= (1 - outer * 0.35)[..., None]
    dy = np.minimum(y, H - 1 - y) / H
    a *= (1 - np.clip(1 - dy * 9, 0, 1) ** 2 * 0.45)[..., None]
    # luz fraca vinda de cima/direita
    d = np.hypot((x - W * 0.62) / W, (y - H * 0.30) / H)
    a *= (1.06 - np.clip(d, 0, 1) * 0.6)[..., None]
    # marca de café na página da direita e mancha na esquerda
    ring = np.hypot(x - W * 0.86, y - H * 0.80)
    rv = (np.exp(-((ring - 115) / 5.5) ** 2) * 0.9 + np.exp(-((ring - 106) / 14) ** 2) * 0.25) * (0.6 + 0.4 * (noise(30, 5) + 0.5))
    a -= (rv * 32)[..., None] * np.array([0.7, 1.0, 1.4])
    spot = np.exp(-(((x - W * 0.16) / 90) ** 2 + ((y - H * 0.12) / 60) ** 2)) * (noise(20, 7) + 0.6)
    a -= (spot * 16)[..., None] * np.array([0.6, 0.9, 1.3])
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).convert('RGBA')
    # margens vermelhas apagadas em cada página
    ov = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d2 = ImageDraw.Draw(ov)
    for mx in (int(W * 0.105), int(cx + W * 0.06)):
        d2.line([(mx, 0), (mx, H)], fill=(150, 62, 56, 60), width=3)
    img = Image.alpha_composite(img, ov.filter(ImageFilter.GaussianBlur(0.7)))
    # contorno levemente irregular, com o meio afundado
    mask = Image.new('L', (W, H), 0); md = ImageDraw.Draw(mask)
    top, bot = [], []
    for i in range(0, W + 1, 10):
        sag = 14 * np.exp(-((i - cx) / 60) ** 2)
        top.append((i, 8 + sag + rng.random() * 4))
        bot.append((i, H - 8 - sag * 0.5 - rng.random() * 4))
    md.polygon(top + bot[::-1], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(1.3))
    img.putalpha(mask)
    return img

if __name__ == '__main__':
    diario().save('diario/diario_aberto.png', optimize=True)
