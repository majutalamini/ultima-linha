"""
Arte do menu inicial (estilo "vagão vermelho"): o mesmo vagão do Cenário 1, visto de frente para a
parede, banhado de luz vermelha. A janela do meio vira um letreiro vermelho marmorizado com o título.
Gera em out/UI/MainMenu/Vermelho/:
  menu_fundo_vermelho.png   1920x1080, vagão + letreiro (sem título)
  menu_titulo.png           1920x1080 transparente, só o título "Última Linha" (gasto)
  menu_brilho_letreiro.png  1920x1080 transparente, brilho do letreiro (pisca de leve na Unity)
  menu_nevoa.png            2048x512, névoa vermelha que se repete na horizontal (desliza na Unity)
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from scipy.ndimage import gaussian_filter, map_coordinates

OUT = 'out/UI/MainMenu/Vermelho'
os.makedirs(OUT, exist_ok=True)
W, H = 1920, 1080
rng = np.random.default_rng(3)
FONT = 'fonts/IMFellEnglish-Regular.ttf'

# recorte do panorama do Cenário 1: janela do meio, alças, banco vermelho e as portas dos lados
CROP = (1330, 230, 2490, 882)
K = W / (CROP[2] - CROP[0])
# vidro da janela no recorte (px do recorte)
WIN = (306, 214 - 60, 866, 486 - 60)
SIGN = tuple(int(v * K) for v in WIN)  # x0, y0, x1, y1 na tela


def noise(shape, sigma, seed):
    r = np.random.default_rng(seed).normal(0, 1, shape)
    n = gaussian_filter(r, sigma, mode='wrap')
    return n / (n.std() + 1e-6)


def fbm(shape, sigmas, seed):
    out = np.zeros(shape)
    for i, s in enumerate(sigmas):
        out += noise(shape, s, seed + i) / (1.6 ** i)
    return out / out.std()


def lerp3(t, stops):
    """t 0..1 -> cor, interpolando entre (pos, (r,g,b))."""
    t = np.clip(t, 0, 1)
    out = np.zeros(t.shape + (3,))
    for (p0, c0), (p1, c1) in zip(stops[:-1], stops[1:]):
        m = (t >= p0) & (t <= p1)
        k = ((t - p0) / max(p1 - p0, 1e-6))[m][:, None]
        out[m] = np.array(c0) * (1 - k) + np.array(c1) * k
    return out


def background():
    src = Image.open('cenario1_fundo.png').convert('RGB').crop(CROP).resize((W, H), Image.LANCZOS)
    a = np.asarray(src).astype(np.float32) / 255
    lum = a @ np.array([0.3, 0.55, 0.15])
    lo, hi = np.percentile(lum, 2), np.percentile(lum, 99.3)
    L = np.clip((lum - lo) / (hi - lo), 0, 1) ** 0.8
    red = lerp3(L, [(0, (14, 3, 4)), (0.35, (92, 14, 14)), (0.7, (176, 40, 34)), (1, (246, 170, 150))])
    # mistura um pouco da cor original (o vermelho dos bancos fica mais vivo)
    orig = a * 255 * 1.9
    img = red * 0.78 + orig * 0.22
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)

    # ---- letreiro no lugar da janela
    x0, y0, x1, y1 = SIGN
    pad = 6
    sx0, sy0, sx1, sy1 = x0 + pad, y0 + pad, x1 - pad, y1 - pad
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle([sx0, sy0, sx1, sy1], radius=int(26 * K), fill=255)
    m = np.asarray(mask.filter(ImageFilter.GaussianBlur(1.2))).astype(np.float32) / 255
    # mármore vermelho: ruído com distorção (redemoinhos)
    sh = (H, W)
    w1, w2 = fbm(sh, (120, 50), 10), fbm(sh, (120, 50), 20)
    base_n = fbm(sh, (70, 28, 9), 30)
    # mármore macio: o ruído é distorcido duas vezes, gerando redemoinhos largos
    u = np.clip(yy + w1 * 90, 0, H - 1)
    v = np.clip(xx + w2 * 90, 0, W - 1)
    swirl = map_coordinates(base_n, [u, v], order=1)
    bands = np.sin(swirl * 2.2 + xx / 260 + yy / 400)
    soft = gaussian_filter(bands, 2)
    streak = gaussian_filter(np.clip(1 - np.abs(soft) * 2.2, 0, 1) ** 2, 7) * 0.32  # veios claros suaves
    t = 0.52 + 0.16 * soft + streak + 0.06 * fbm(sh, (3, 1), 33)
    marble = lerp3(np.clip(t, 0, 1), [(0, (110, 8, 10)), (0.4, (170, 14, 16)), (0.62, (206, 28, 26)), (0.85, (232, 84, 74)), (1, (246, 150, 132))])
    # pontilhado de impressão (meio-tom) bem leve
    dots = (np.sin(xx * 0.9) * np.sin(yy * 0.9) > 0.55) * 0.06
    marble *= (1 - dots)[..., None]
    # luz: centro mais claro, bordas mais fechadas
    cx, cy = (sx0 + sx1) / 2, (sy0 + sy1) / 2
    d = np.sqrt(((xx - cx) / (sx1 - sx0)) ** 2 + ((yy - cy) / (sy1 - sy0)) ** 2)
    marble *= np.clip(1.12 - d * 0.75, 0.55, 1.1)[..., None]
    # moldura: sombra interna
    inner = gaussian_filter(1 - np.asarray(mask).astype(np.float32) / 255, 10)
    marble *= (1 - np.clip(inner * 1.8, 0, 0.6))[..., None]
    img = img * (1 - m[..., None]) + marble * m[..., None]
    # moldura metálica clara em volta do letreiro (como a moldura da janela, mas iluminada)
    frame = Image.new('L', (W, H), 0)
    ImageDraw.Draw(frame).rounded_rectangle([x0 - 4, y0 - 4, x1 + 4, y1 + 4], radius=int(30 * K), outline=255, width=9)
    f = np.asarray(frame.filter(ImageFilter.GaussianBlur(1.0))).astype(np.float32) / 255
    img = img * (1 - f[..., None] * 0.55) + np.array([168, 120, 112]) * (f[..., None] * 0.55)

    # ---- o letreiro ilumina o vagão (vermelho), o resto afunda nas bordas
    glow = gaussian_filter(m, 120)
    glow = glow / glow.max()
    img = img + np.array([120, 18, 14]) * (glow ** 1.2 * 0.55)[..., None] * (1 - m[..., None])
    vig = np.sqrt(((xx - W / 2) / (W * 0.62)) ** 2 + ((yy - H * 0.45) / (H * 0.72)) ** 2)
    img *= np.clip(1.15 - vig ** 2.2 * 0.75, 0.25, 1)[..., None]
    # textura de papel/pintura (parada) + granulado fino
    paper = fbm(sh, (3, 1.2), 50) * 0.05 + fbm(sh, (24, 8), 60) * 0.05
    img *= (1 + paper)[..., None]
    img += rng.normal(0, 5, (H, W, 1))
    # leve aberração cromática nas bordas (cara de foto/scan)
    out = np.clip(img, 0, 255).astype(np.uint8)
    im = Image.fromarray(out, 'RGB')
    r, g, b = im.split()
    r = r.transform(im.size, Image.AFFINE, (1.0015, 0, -1.4, 0, 1.0015, -0.8), Image.BILINEAR)
    im = Image.merge('RGB', (r, g, b))
    im.save(f'{OUT}/menu_fundo_vermelho.png')
    return m


def title():
    x0, y0, x1, y1 = SIGN
    size = 132
    font = ImageFont.truetype(FONT, size)
    text = 'Última Linha'
    layer = Image.new('L', (W, H), 0)
    d = ImageDraw.Draw(layer)
    bbox = d.textbbox((0, 0), text, font=font)
    tx = x0 + (x1 - x0) * 0.07
    ty = y0 + (y1 - y0) * 0.36 - (bbox[3] - bbox[1]) * 0.5
    d.text((tx, ty), text, font=font, fill=255)
    a = np.asarray(layer).astype(np.float32) / 255
    # tinta gasta: falhas e respingos dentro das letras
    wear = fbm((H, W), (1.2, 3), 70)
    a = a * np.clip(1.0 - np.clip(wear - 1.3, 0, None) * 1.2, 0.35, 1)
    a = np.clip(a, 0, 1)
    # leve "tremida" de borda (pintado à mão)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    jx, jy = noise((H, W), 6, 80) * 1.4, noise((H, W), 6, 81) * 1.4
    a = map_coordinates(a, [yy + jy, xx + jx], order=1)
    rgb = np.zeros((H, W, 3), np.float32) + np.array([238, 226, 214])
    rgb *= (1 - 0.12 * np.clip(fbm((H, W), (8, 30), 90), -1, 1))[..., None]
    # sombra suave escura atrás das letras
    sh = gaussian_filter(a, 5) * 0.55
    sh = np.roll(np.roll(sh, 4, 0), 3, 1)
    out_a = a + sh * (1 - a)
    col = (rgb * a[..., None] + np.array([40, 0, 0]) * (sh * (1 - a))[..., None]) / np.maximum(out_a, 1e-4)[..., None]
    img = np.dstack([np.clip(col, 0, 255), np.clip(out_a, 0, 1) * 255]).astype(np.uint8)
    Image.fromarray(img, 'RGBA').save(f'{OUT}/menu_titulo.png')


def sign_glow(m):
    g = gaussian_filter(m, 60) * 1.0 + gaussian_filter(m, 18) * 0.4
    g = np.clip(g / g.max(), 0, 1) * (1 - m * 0.85)
    img = np.dstack([np.full((H, W), 255), np.full((H, W), 60), np.full((H, W), 48), g * 255 * 0.55]).astype(np.uint8)
    Image.fromarray(img, 'RGBA').save(f'{OUT}/menu_brilho_letreiro.png')


def fog():
    w, h = 2048, 512
    n = np.zeros((h, w))
    for i, s in enumerate((90, 40)):
        r = np.random.default_rng(100 + i).normal(0, 1, (h, w))
        n += gaussian_filter(r, (s * 0.6, s * 1.6), mode='wrap') / (1.7 ** i)
    n = (n - n.min()) / (n.max() - n.min())
    yy = np.arange(h)[:, None] / h
    a = np.clip(n * 1.6 - 0.35, 0, 1) * np.clip((yy - 0.05) / 0.75, 0, 1) ** 1.2
    a = np.clip(a + np.clip((yy - 0.75) / 0.25, 0, 1) * 0.5, 0, 1)
    col = lerp3(n, [(0, (120, 10, 10)), (0.6, (200, 30, 26)), (1, (240, 90, 76))])
    img = np.dstack([col, a * 255 * 0.7]).astype(np.uint8)
    Image.fromarray(img, 'RGBA').save(f'{OUT}/menu_nevoa.png')


if __name__ == '__main__':
    m = background()
    title()
    sign_glow(m)
    fog()
    # prévia montada (com névoa e os itens do menu, para conferir)
    bg = Image.open(f'{OUT}/menu_fundo_vermelho.png').convert('RGBA')
    bg.alpha_composite(Image.open(f'{OUT}/menu_brilho_letreiro.png'))
    bg.alpha_composite(Image.open(f'{OUT}/menu_titulo.png'))
    fogi = Image.open(f'{OUT}/menu_nevoa.png').resize((W, 420))
    bg.alpha_composite(fogi, (0, H - 420))
    d = ImageDraw.Draw(bg)
    f = ImageFont.truetype(FONT, 44)
    items = ['Começar', 'Continuar', 'Configurações', 'Sair']
    y = 742
    for i, t in enumerate(items):
        tw = d.textlength(t, font=f)
        if i == 1:
            bar = Image.new('RGBA', (400, 58), (245, 225, 220, 80))
            bg.alpha_composite(bar, (int(W / 2 - 200), y - 4))
        d.text((W / 2 - tw / 2, y), t, font=f, fill=(250, 240, 235, 255))
        y += 66
    bg.convert('RGB').save('menu/previa_menu.png')
