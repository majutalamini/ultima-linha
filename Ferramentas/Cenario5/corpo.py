"""O corpo do chefe (Cenário 5): o último quadro da queda do Souta (fallen_05, desenhado à mão, caído de lado com a
cabeça à direita), repintado para ser outra pessoa: terno escuro no lugar da camisa clara, cabelo grisalho e pele pálida,
sem cor de vida. A cabeça vira um sprite à parte (ela se solta quando o Kai atravessa o corpo) e uma poça de sangue
irregular vai por baixo, mais funda perto da cabeça. Grava em Assets/Art/Cenarios4a7: corpo_chefe.png, cabeca_chefe.png,
sangue_chefe.png, e imprime onde a cabeça fica em relação ao meio do corpo (px da arte, 58 por unidade).
Uso: python Ferramentas/Cenario5/corpo.py"""
import colorsys
import os

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ORIGEM = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt", "Souta", "fallen_05.png")
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Cenarios4a7")

# regiões no recorte do corpo (x, y a partir do canto do bbox): a cabeça à direita, as mãos embaixo à direita
CABECA_X = 112          # da coluna 112 para a direita é cabeça (e as mãos junto do rosto)
MAOS_Y = 36             # abaixo disso, perto da cabeça, são as mãos/antebraços
TRONCO_X0 = 58          # o tronco (a camisa clara) começa aqui


def hls(c):
    return colorsys.rgb_to_hls(*(c / 255.0))


def cor(h, l, s):
    return np.array(colorsys.hls_to_rgb(h, l, s)) * 255.0


def repinta(a):
    out = a.copy().astype(float)
    hh, ww = a.shape[:2]
    for y in range(hh):
        for x in range(ww):
            if a[y, x, 3] == 0:
                continue
            h, l, s = hls(a[y, x, :3].astype(float))
            cabeca = x >= CABECA_X
            maos = x >= CABECA_X - 20 and y >= MAOS_Y
            if cabeca and not maos and y < 22 and l < 0.42:
                # cabelo: de castanho para grisalho, mantendo os fios escuros (contraste do desenho original)
                out[y, x, :3] = cor(0.08, 0.16 + l * 1.05, 0.03)
            elif (cabeca or maos) and l >= 0.42:
                # pele: pálida, fria, sem sangue
                out[y, x, :3] = cor(0.08, min(0.80, l * 1.02), 0.08) * np.array([0.93, 0.95, 1.0])
            elif x >= TRONCO_X0 and not cabeca and l >= 0.42:
                # a camisa clara vira o paletó do terno: chumbo
                out[y, x, :3] = cor(0.6, 0.14 + (l - 0.42) * 0.6, 0.06)
            elif l < 0.42:
                # calça e sombras: o mesmo chumbo do paletó (terno inteiro)
                out[y, x, :3] = cor(0.6, l * 0.95, 0.05)
    # um fio de camisa branca e a gravata vermelha-escura na gola (perto da cabeça)
    return out.clip(0, 255).astype(np.uint8)


def poca(w, h):
    """Poça de sangue irregular, mais larga e funda perto da cabeça (à direita), com o brilho do molhado."""
    rng = np.random.default_rng(5)
    yy, xx = np.mgrid[0:h, 0:w]
    m = np.zeros((h, w), bool)
    # blocos elípticos sobrepostos, crescendo para a direita
    for cx, rx, ry in ((0.78, 0.2, 0.5), (0.6, 0.2, 0.44), (0.42, 0.16, 0.34), (0.27, 0.1, 0.24), (0.92, 0.07, 0.36)):
        m |= ((xx - w * cx) / (w * rx)) ** 2 + ((yy - h * 0.5) / (h * ry)) ** 2 < 1
    borda = m & ~(np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1))
    s = np.zeros((h, w, 4), np.uint8)
    s[m] = (62, 8, 8, 240)
    # mais fundo no meio de cada mancha (degradê suave, sem emenda)
    d = np.zeros((h, w))
    for cx, rx, ry in ((0.78, 0.2, 0.5), (0.6, 0.2, 0.44), (0.42, 0.16, 0.34)):
        d = np.maximum(d, 1 - (((xx - w * cx) / (w * rx)) ** 2 + ((yy - h * 0.5) / (h * ry)) ** 2))
    fundo = m & (d > 0.45)
    s[fundo] = (48, 5, 5, 245)
    s[borda] = (86, 14, 12, 230)                           # a borda pega luz
    # reflexo do molhado: alguns pixels claros soltos, não uma linha
    for _ in range(14):
        x = int(rng.uniform(0.3, 0.9) * w); y = int(rng.uniform(0.25, 0.45) * h)
        if m[y, x]:
            s[y, x] = (112, 26, 22, 235)
    # respingos soltos
    for _ in range(9):
        x = int(rng.uniform(0.05, 1.0) * (w - 2))
        y = int(rng.uniform(0.2, 0.8) * (h - 1))
        s[y, x:x + 2] = (70, 10, 9, 230)
    return s


def main():
    im = Image.open(ORIGEM).convert("RGBA")
    im = im.crop(im.getbbox())
    a = np.array(im)
    a = repinta(a)
    h, w = a.shape[:2]
    corpo, cabeca = a.copy(), a.copy()
    corpo[:, CABECA_X:] = 0
    cabeca[:, :CABECA_X] = 0
    # as mãos ficam no corpo (só a cabeça se solta): da cabeça sai o que está abaixo do pescoço
    cabeca[MAOS_Y:, :] = 0
    corpo[MAOS_Y:, CABECA_X:] = a[MAOS_Y:, CABECA_X:]
    # cada um recortado no próprio retângulo; anota onde o meio de cada um cai
    def recorta(img):
        ys, xs = np.nonzero(img[..., 3])
        return img[ys.min():ys.max() + 1, xs.min():xs.max() + 1], ((xs.min() + xs.max() + 1) / 2, (ys.min() + ys.max() + 1) / 2)
    corpo_r, cc = recorta(corpo)
    cabeca_r, hc = recorta(cabeca)
    Image.fromarray(corpo_r, "RGBA").save(os.path.join(SAIDA, "corpo_chefe.png"))
    Image.fromarray(cabeca_r, "RGBA").save(os.path.join(SAIDA, "cabeca_chefe.png"))
    Image.fromarray(poca(int(w * 0.95), 24), "RGBA").save(os.path.join(SAIDA, "sangue_chefe.png"))
    # deslocamento da cabeça em relação ao meio do corpo (px da arte; y para baixo)
    print(f"corpo {corpo_r.shape[1]}x{corpo_r.shape[0]}  cabeça {cabeca_r.shape[1]}x{cabeca_r.shape[0]}  "
          f"cabeça-corpo dx={hc[0] - cc[0]:.1f} dy={hc[1] - cc[1]:.1f}  base do corpo {h - cc[1]:.1f} px abaixo do meio")


if __name__ == "__main__":
    main()
