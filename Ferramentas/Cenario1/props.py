"""Gera os sprites de apoio do Cenário 1 (bagageiro, alavanca, lâmpadas, portas, tecla E...)."""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = 'out/Art/Cenario1'
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(7)
EXT = 160  # faixa adicionada no topo do fundo

FUNDO = Image.open('cenario1_fundo.png').convert('RGB')


def save(img, name):
    img.save(f'{OUT}/{name}.png', optimize=True)


def grime(img, amount=18, seed=0):
    """Ruído e manchas para o metal não ficar liso demais."""
    r = np.random.default_rng(seed)
    a = np.asarray(img).astype(np.float32)
    n = r.normal(0, amount, a.shape[:2])[..., None]
    blot = np.asarray(Image.fromarray((r.random(a.shape[:2]) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4))).astype(np.float32)
    blot = (blot - blot.mean()) / (blot.std() + 1e-6)
    a[..., :3] = a[..., :3] + n + blot[..., None] * amount * 0.6
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), img.mode)


# ---------------- Bagageiro (prateleira de bagagem) ----------------
def bagageiro(w=340, h=58):
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    steel, dark, hi = (88, 84, 76, 255), (34, 33, 31, 255), (168, 162, 150, 255)
    # suportes até a parede
    for x in (14, w // 2 - 4, w - 22):
        d.polygon([(x, 0), (x + 8, 0), (x + 8, 24), (x, 30)], fill=dark)
        d.line([(x + 1, 0), (x + 1, 26)], fill=(70, 67, 61, 255))
    # rede (losangos)
    for x in range(-h, w + h, 9):
        d.line([(x, 14), (x + 14, 34)], fill=(60, 57, 52, 200))
        d.line([(x + 14, 14), (x, 34)], fill=(60, 57, 52, 200))
    # tubo da frente (onde se pisa)
    d.rectangle([0, 6, w - 1, 14], fill=steel)
    d.line([(0, 7), (w - 1, 7)], fill=hi)
    d.line([(0, 14), (w - 1, 14)], fill=dark)
    # tubo inferior
    d.rectangle([4, 32, w - 5, 38], fill=(62, 59, 54, 255))
    d.line([(4, 33), (w - 5, 33)], fill=(120, 115, 105, 255))
    # objetos esquecidos em cima (mala, sacola)
    d.rectangle([w * 0.62, -2 + 0, w * 0.62 + 46, 6], fill=(40, 30, 28, 255))
    im = grime(im, 10, 3)
    # sombra suave embaixo
    sh = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(sh).rectangle([6, 40, w - 6, 50], fill=(0, 0, 0, 110))
    sh = sh.filter(ImageFilter.GaussianBlur(5))
    return Image.alpha_composite(sh, im)


# ---------------- Alavanca ----------------
def alavanca_base(w=78, h=52):
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([2, 8, w - 3, h - 1], fill=(46, 44, 40, 255))
    d.rectangle([2, 8, w - 3, 12], fill=(96, 92, 84, 255))
    # faixa de aviso amarela e preta
    for i in range(-h, w, 12):
        d.polygon([(i, h - 14), (i + 6, h - 14), (i + 12, h - 3), (i + 6, h - 3)], fill=(150, 118, 30, 255))
    d.rectangle([2, h - 15, w - 3, h - 15], fill=(20, 20, 18, 255))
    d.rectangle([2, h - 3, w - 3, h - 1], fill=(20, 20, 18, 255))
    # trilho por onde o cabo passa
    d.rectangle([w // 2 - 22, 10, w // 2 + 22, 15], fill=(14, 14, 13, 255))
    # parafusos
    for x in (7, w - 9):
        d.ellipse([x, 18, x + 3, 21], fill=(130, 125, 115, 255))
    return grime(im, 9, 5)


def alavanca_cabo(w=20, h=96):
    """Cabo com pivô na base (o sprite é importado com pivô embaixo)."""
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = w // 2
    d.rectangle([cx - 3, 14, cx + 3, h - 1], fill=(120, 116, 108, 255))
    d.line([(cx - 2, 14), (cx - 2, h - 1)], fill=(190, 184, 170, 255))
    d.line([(cx + 3, 14), (cx + 3, h - 1)], fill=(52, 50, 46, 255))
    # manopla vermelha
    d.rounded_rectangle([cx - 8, 0, cx + 8, 20], radius=6, fill=(122, 22, 20, 255))
    d.rounded_rectangle([cx - 6, 2, cx - 1, 15], radius=3, fill=(176, 58, 46, 255))
    return im


def lampada(s=22):
    im = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([2, 2, s - 3, s - 3], fill=(255, 255, 255, 255))
    # grade de proteção
    for x in (s // 3, 2 * s // 3):
        d.line([(x, 2), (x, s - 3)], fill=(30, 30, 30, 200))
    d.line([(2, s // 2), (s - 3, s // 2)], fill=(30, 30, 30, 200))
    return im


def soquete(w=26, h=14):
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, w - 1, h - 1], fill=(40, 38, 35, 255))
    d.line([(0, 0), (w - 1, 0)], fill=(110, 105, 96, 255))
    return im


def brilho(s=256, power=2.0):
    y, x = np.mgrid[0:s, 0:s]
    r = np.hypot(x - s / 2 + 0.5, y - s / 2 + 0.5) / (s / 2)
    a = np.clip(1 - r, 0, 1) ** power
    arr = np.zeros((s, s, 4), np.uint8)
    arr[..., :3] = 255
    arr[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(arr, 'RGBA')


def brilho_tubo(w=256, h=64):
    """Halo alongado para as luminárias fluorescentes."""
    y, x = np.mgrid[0:h, 0:w]
    dx = np.clip(np.abs(x - w / 2 + 0.5) - w * 0.28, 0, None) / (w * 0.22)
    dy = np.abs(y - h / 2 + 0.5) / (h / 2)
    a = np.clip(1 - np.hypot(dx, dy), 0, 1) ** 1.8
    arr = np.zeros((h, w, 4), np.uint8)
    arr[..., :3] = 255
    arr[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(arr, 'RGBA')


def ponto(s=12):
    im = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    ImageDraw.Draw(im).ellipse([1, 1, s - 2, s - 2], fill=(255, 255, 255, 255))
    return im


def tecla(letra='E', s=56):
    im = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([2, 4, s - 3, s - 3], radius=9, fill=(12, 12, 12, 210))
    d.rounded_rectangle([2, 2, s - 3, s - 7], radius=9, fill=(30, 30, 28, 235), outline=(200, 192, 176, 255), width=2)
    f = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 26)
    tw = d.textlength(letra, font=f)
    d.text(((s - tw) / 2, 9), letra, font=f, fill=(232, 225, 212, 255))
    return im


def sinal_porta(w=46, h=26):
    """Caixinha da lâmpada de trava em cima da porta de saída."""
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, w - 1, h - 1], fill=(30, 29, 27, 255))
    d.rectangle([0, 0, w - 1, 2], fill=(98, 94, 86, 255))
    d.rectangle([4, 5, w - 5, h - 5], fill=(10, 10, 10, 255))
    return im


def pixel(s=8):
    return Image.new('RGBA', (s, s), (255, 255, 255, 255))


# ---------------- Porta de saída: folhas recortadas do próprio fundo ----------------
DOOR_X0, DOOR_SPLIT, DOOR_X1 = 3730, 3904, 4080   # em pixels do fundo (trecho 3)
DOOR_Y0, DOOR_Y1 = 212 + EXT, 746 + EXT


def folhas():
    esq = FUNDO.crop((DOOR_X0, DOOR_Y0, DOOR_SPLIT, DOOR_Y1)).convert('RGBA')
    dir_ = FUNDO.crop((DOOR_SPLIT, DOOR_Y0, DOOR_X1, DOOR_Y1)).convert('RGBA')
    return esq, dir_


def interior(w=DOOR_X1 - DOOR_X0, h=DOOR_Y1 - DOOR_Y0):
    """O que aparece quando a porta abre: o próximo vagão, escuro, com uma luz fraca lá no fundo."""
    y, x = np.mgrid[0:h, 0:w]
    a = np.zeros((h, w, 3), np.float32) + [5, 6, 7]
    # corredor em perspectiva: luz fraca no centro
    cx, cy = w * 0.5, h * 0.42
    g = np.exp(-(((x - cx) / (w * 0.22)) ** 2 + ((y - cy) / (h * 0.30)) ** 2))
    a += g[..., None] * [34, 38, 36]
    # piso refletindo
    fl = np.clip((y - h * 0.72) / (h * 0.28), 0, 1) * np.exp(-((x - cx) / (w * 0.18)) ** 2)
    a += fl[..., None] * [22, 24, 22]
    # luz fluorescente distante
    a[int(h * 0.18):int(h * 0.18) + 3, int(w * 0.40):int(w * 0.60)] = [120, 128, 122]
    a += rng.normal(0, 3, a.shape)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).convert('RGBA')


def vinheta(s=512):
    y, x = np.mgrid[0:s, 0:s]
    d = np.hypot(x - s / 2 + 0.5, y - s / 2 + 0.5) / (s / 2)
    t = np.clip((d - 0.45) / 0.95, 0, 1)
    a = t * t * (3 - 2 * t)
    arr = np.zeros((s, s, 4), np.uint8)
    arr[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(arr, 'RGBA')


if __name__ == '__main__':
    save(FUNDO, 'cenario1_fundo')
    save(bagageiro(), 'bagageiro')
    save(alavanca_base(), 'alavanca_base')
    save(alavanca_cabo(), 'alavanca_cabo')
    save(lampada(), 'lampada')
    save(soquete(), 'lampada_soquete')
    save(brilho(), 'brilho')
    save(brilho_tubo(), 'brilho_tubo')
    save(ponto(), 'ponto')
    save(tecla('E'), 'tecla_e')
    save(sinal_porta(), 'sinal_porta')
    save(pixel(), 'pixel')
    e, d = folhas()
    save(e, 'porta_folha_esq')
    save(d, 'porta_folha_dir')
    save(interior(), 'porta_interior')
    save(vinheta(), 'vinheta')
    print('ok')
