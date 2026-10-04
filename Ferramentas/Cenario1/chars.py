"""Personagens provisórios (Kai, Souta, Ren) desenhados de perfil, virados para a direita.
Servem até a arte definitiva ser colocada em Assets/Art/Personagens (o builder usa a definitiva se existir)."""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = 'out/Art/Cenario1/Personagens'
os.makedirs(OUT, exist_ok=True)
S = 3  # desenha em 3x e reduz (bordas suaves)


def P(pts, ox=0, oy=0):
    return [((x + ox) * S, (y + oy) * S) for x, y in pts]


def ell(d, cx, cy, rx, ry, fill):
    d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=fill)


def finish(img, w, h, rim=(200, 190, 170), rim_side=1):
    """Reduz, adiciona luz de contorno (vinda da janela) e uma sombra de contato."""
    small = img.resize((w, h), Image.LANCZOS)
    a = np.asarray(small).astype(np.float32)
    alpha = a[..., 3] / 255.0
    # contorno de luz: onde há personagem mas o vizinho (do lado da luz) é vazio
    shifted = np.roll(alpha, -2 * rim_side, axis=1)
    shifted_up = np.roll(alpha, 2, axis=0)
    edge = np.clip(alpha - np.minimum(shifted, 1.0), 0, 1) * 0.55 + np.clip(alpha - shifted_up, 0, 1) * 0.35
    edge = np.asarray(Image.fromarray((edge * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))).astype(np.float32) / 255
    for c in range(3):
        a[..., c] = a[..., c] * (1 - edge) + rim[c] * edge
    # escurece levemente as pernas (luz vem de cima)
    yy = np.linspace(0, 1, h)[:, None]
    shade = 1 - np.clip((yy - 0.55) / 0.45, 0, 1) * 0.35
    a[..., :3] *= shade[..., None]
    out = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA')
    return out


# Cores
PELE = (150, 118, 98, 255)
PELE_S = (112, 86, 72, 255)
CAMISA = (120, 122, 120, 255)
CAMISA_S = (88, 90, 90, 255)
CALCA = (30, 31, 36, 255)
CALCA_S = (22, 22, 26, 255)
SAPATO = (16, 15, 15, 255)
CABELO_K = (20, 19, 21, 255)
CABELO_S = (66, 46, 32, 255)
CABELO_R = (176, 160, 120, 255)
GRAVATA = (92, 24, 26, 255)
CARDIGA = (40, 40, 46, 255)
CARDIGA_S = (30, 30, 35, 255)


def kai():
    """Cansado: cabeça baixa e para frente, ombros caídos, mãos nos bolsos, gravata frouxa."""
    W, H = 150, 340
    im = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # perna de trás e da frente (levemente afastadas)
    d.polygon(P([(52, 196), (76, 196), (74, 326), (58, 326)]), fill=CALCA_S)
    d.polygon(P([(70, 196), (96, 196), (100, 326), (82, 326)]), fill=CALCA)
    # sapatos
    d.polygon(P([(52, 322), (76, 322), (82, 336), (50, 336)]), fill=SAPATO)
    d.polygon(P([(80, 322), (102, 322), (114, 330), (114, 337), (80, 337)]), fill=SAPATO)
    # tronco (camisa amassada, para fora da calça)
    d.polygon(P([(48, 96), (96, 92), (104, 150), (100, 206), (50, 206), (44, 150)]), fill=CAMISA)
    d.polygon(P([(48, 96), (62, 95), (58, 206), (50, 206), (44, 150)]), fill=CAMISA_S)
    # dobras
    for y in (128, 160, 186):
        d.line(P([(60, y), (94, y + 6)]), fill=CAMISA_S, width=2 * S)
    # braço (mão no bolso)
    d.polygon(P([(62, 100), (82, 100), (88, 150), (86, 192), (70, 194), (66, 150)]), fill=CAMISA_S)
    d.polygon(P([(70, 186), (88, 186), (90, 200), (72, 202)]), fill=CALCA_S)
    # gravata frouxa, torta
    d.polygon(P([(88, 100), (96, 100), (100, 112), (104, 160), (96, 166), (92, 114)]), fill=GRAVATA)
    # pescoço e cabeça para a frente
    d.polygon(P([(70, 84), (88, 82), (94, 100), (72, 102)]), fill=PELE_S)
    ell(d, 86, 66, 24, 27, PELE)
    d.polygon(P([(104, 66), (112, 74), (106, 78)]), fill=PELE)          # nariz
    d.line(P([(96, 64), (106, 66)]), fill=(40, 30, 28, 255), width=2 * S)  # olho semicerrado
    d.line(P([(96, 70), (104, 71)]), fill=(70, 50, 52, 255), width=1 * S)  # olheira
    d.line(P([(98, 84), (106, 83)]), fill=(80, 54, 48, 255), width=1 * S)  # boca
    # cabelo desgrenhado
    d.polygon(P([(58, 60), (60, 42), (72, 34), (90, 34), (106, 42), (114, 56), (104, 50), (108, 62),
                 (96, 52), (98, 64), (86, 54), (80, 66), (72, 62), (66, 80), (60, 72)]), fill=CABELO_K)
    d.polygon(P([(70, 36), (74, 26), (80, 34), (88, 24), (92, 34)]), fill=CABELO_K)
    return finish(im, W, H)


def souta():
    """Confiante: postura ereta, peito aberto, cabelo penteado, gravata com prendedor."""
    W, H = 150, 352
    im = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon(P([(50, 206), (74, 206), (70, 338), (54, 338)]), fill=(26, 28, 40, 255))
    d.polygon(P([(70, 206), (96, 206), (102, 338), (84, 338)]), fill=(36, 38, 54, 255))
    d.polygon(P([(48, 334), (72, 334), (78, 348), (46, 348)]), fill=SAPATO)
    d.polygon(P([(82, 334), (104, 334), (118, 342), (118, 349), (82, 349)]), fill=SAPATO)
    # cinto
    d.rectangle([48 * S, 200 * S, 98 * S, 208 * S], fill=(20, 18, 16, 255))
    d.rectangle([90 * S, 201 * S, 96 * S, 207 * S], fill=(150, 140, 110, 255))
    # tronco (camisa impecável, por dentro)
    d.polygon(P([(44, 92), (96, 92), (102, 140), (98, 202), (50, 202), (46, 140)]), fill=(168, 170, 168, 255))
    d.polygon(P([(44, 92), (58, 92), (56, 202), (50, 202), (46, 140)]), fill=(128, 130, 130, 255))
    # braço relaxado ao lado
    d.polygon(P([(58, 98), (78, 98), (80, 150), (78, 196), (64, 196), (60, 150)]), fill=(140, 142, 142, 255))
    ell(d, 71, 202, 8, 9, PELE)
    # gravata reta + prendedor metálico
    d.polygon(P([(92, 96), (98, 96), (100, 106), (100, 170), (95, 178), (91, 170), (91, 106)]), fill=(34, 44, 72, 255))
    d.rectangle([88 * S, 140 * S, 102 * S, 142 * S], fill=(210, 205, 190, 255))
    # pescoço e cabeça erguida
    d.polygon(P([(66, 78), (84, 78), (86, 94), (68, 94)]), fill=PELE_S)
    ell(d, 80, 56, 24, 28, PELE)
    d.polygon(P([(100, 56), (108, 64), (101, 67)]), fill=PELE)
    d.line(P([(90, 52), (99, 51)]), fill=(30, 24, 22, 255), width=2 * S)    # olhar firme
    d.line(P([(89, 46), (100, 44)]), fill=(52, 36, 26, 255), width=2 * S)   # sobrancelha
    d.line(P([(92, 74), (100, 73)]), fill=(90, 58, 50, 255), width=1 * S)
    # cabelo penteado de lado
    d.polygon(P([(56, 54), (56, 36), (66, 26), (84, 24), (100, 30), (106, 42), (92, 36), (70, 38), (64, 50), (60, 62)]), fill=CABELO_S)
    d.line(P([(70, 30), (96, 32)]), fill=(96, 70, 50, 255), width=2 * S)
    return finish(im, W, H)


def ren():
    """Medo: encolhido, ombros pra cima, mãos juntas na frente, óculos, cardigã largo."""
    W, H = 150, 326
    im = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon(P([(56, 196), (78, 196), (74, 312), (60, 312)]), fill=CALCA_S)
    d.polygon(P([(72, 196), (94, 196), (92, 312), (78, 312)]), fill=CALCA)
    d.polygon(P([(56, 308), (76, 308), (80, 322), (54, 322)]), fill=SAPATO)
    d.polygon(P([(76, 308), (94, 308), (104, 316), (104, 323), (76, 323)]), fill=SAPATO)
    # cardigã largo, comprido
    d.polygon(P([(42, 104), (94, 100), (104, 150), (106, 214), (44, 216), (40, 150)]), fill=CARDIGA)
    d.polygon(P([(42, 104), (56, 102), (52, 216), (44, 216), (40, 150)]), fill=CARDIGA_S)
    # camisa aparecendo na frente
    d.polygon(P([(90, 104), (98, 104), (104, 150), (104, 196), (96, 196)]), fill=(110, 112, 110, 255))
    # braços dobrados, mãos juntas na frente da barriga
    d.polygon(P([(58, 106), (80, 106), (92, 150), (104, 160), (100, 172), (82, 164), (66, 140)]), fill=CARDIGA_S)
    ell(d, 104, 166, 9, 8, PELE)
    ell(d, 98, 170, 8, 7, PELE_S)
    # cabeça baixa, encolhida entre os ombros
    d.polygon(P([(70, 96), (86, 94), (88, 108), (72, 108)]), fill=PELE_S)
    ell(d, 84, 78, 23, 26, PELE)
    d.polygon(P([(104, 82), (110, 90), (104, 92)]), fill=PELE)
    # óculos (aro claro) e olhar baixo
    d.rectangle([92 * S, 76 * S, 106 * S, 85 * S], outline=(196, 196, 186, 255), width=2 * S)
    d.line(P([(96, 83), (102, 84)]), fill=(30, 26, 24, 255), width=2 * S)
    d.line(P([(96, 98), (102, 97)]), fill=(90, 60, 54, 255), width=1 * S)
    # cabelo claro caindo na testa
    d.polygon(P([(60, 80), (60, 60), (70, 52), (88, 50), (104, 58), (108, 72), (98, 66), (92, 76), (86, 66), (78, 76), (70, 70), (66, 90)]), fill=CABELO_R)
    d.line(P([(70, 56), (100, 62)]), fill=(210, 196, 156, 255), width=2 * S)
    # ombros encolhidos
    d.polygon(P([(42, 104), (52, 92), (70, 96), (60, 108)]), fill=CARDIGA)
    return finish(im, W, H)


def retrato(fig, top, height):
    """Recorte cabeça-e-ombros para a caixa de diálogo, ampliado."""
    w = fig.width
    crop = fig.crop((0, top, w, top + height))
    return crop.resize((crop.width * 3, crop.height * 3), Image.LANCZOS)


if __name__ == '__main__':
    k, s, r = kai(), souta(), ren()
    k.save(f'{OUT}/kai_cena.png')
    s.save(f'{OUT}/souta_cena.png')
    r.save(f'{OUT}/ren_cena.png')
    retrato(k, 18, 130).save(f'{OUT}/kai_retrato_provisorio.png')
    retrato(s, 10, 130).save(f'{OUT}/souta_retrato_provisorio.png')
    retrato(r, 36, 130).save(f'{OUT}/ren_retrato_provisorio.png')
    sheet = Image.new('RGBA', (520, 360), (52, 50, 48, 255))
    for i, im in enumerate((k, s, r)):
        sheet.alpha_composite(im, (10 + i * 170, 360 - im.height - 4))
    sheet.save('chars_sheet.png')
    print('ok')
