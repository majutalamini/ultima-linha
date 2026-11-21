"""O chefe no Cenário 5, pendurado pelo pescoço no cano do teto: a cena foi montada para parecer suicídio
(a corda, a cadeira chutada), mas ele foi estrangulado antes. A cabeça fica presa ao corpo.

O corpo vem do passageiro de terno (Passageiros/pe_1, desenhado à mão, de perfil), sem a pasta na mão, com o braço
caído, a cabeça tombada para frente, os pés apontando para baixo, o cabelo grisalho e a pele pálida.
A corda sai do cano do teto, passa por trás da cabeça e dá a volta no pescoço.

Grava em Assets/Art/Cenarios4a7:
- enforcado.png     corda + corpo, com o pivô no topo (o ponto onde a corda abraça o cano)
- cadeira_caida.png a cadeira tombada no chão, pivô embaixo
- sangue_chefe.png  a poça embaixo dos pés
Uso: python Ferramentas/Cenario5/enforcado.py"""
import colorsys
import os

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ORIGEM = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt", "Passageiros", "pe_1.png")
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Cenarios4a7")

# medidas no painel (px da arte, 58 por unidade; y de cima para baixo, o chão fica em 585)
CANO_TOPO = 147      # embaixo do cano do teto, onde a corda está amarrada
CANO_BAIXO = 154
CHAO = 585
PES_DO_CHAO = 52     # o quanto os pés ficam acima do chão (a altura do assento da cadeira chutada)

# regiões no recorte do pe_1 (46 x 222)
PASTA_Y0, PASTA_Y1 = 133, 164     # a pasta na mão (sai, e as pernas são refeitas por trás)
CABECA_Y1 = 35                    # do topo até aqui é cabeça
PESCOCO_Y = 37                    # onde passa o laço
SAPATO_Y0 = 211                   # dos sapatos para baixo
TORNOZELO_X = 16                  # onde o sapato gira
PE_GIRO = 32                      # graus, ponta para baixo

CORDA = [(52, 43, 31), (88, 75, 54), (114, 98, 71)]   # sombra, meio, luz


def hls(c):
    return colorsys.rgb_to_hls(*(c / 255.0))


def cor(h, l, s):
    return np.array(colorsys.hls_to_rgb(h, l, s)) * 255.0


def tira_pasta(a):
    """Apaga a pasta e refaz as pernas atrás dela, esticando a calça de baixo até a de cima."""
    a = a.copy()
    def borda(row):
        xs = np.nonzero(a[row, :, 3] > 128)[0]
        return xs.min(), xs.max()
    cima = (13, 38)                    # calça logo acima da pasta (à direita da mão)
    baixo = borda(PASTA_Y1 + 4)
    modelo = a[PASTA_Y1 + 4].copy()
    m0, m1 = baixo
    for y in range(PASTA_Y0, PASTA_Y1 + 1):
        t = (y - PASTA_Y0) / (PASTA_Y1 - PASTA_Y0)
        x0 = int(round(cima[0] + (m0 - cima[0]) * t))
        x1 = int(round(cima[1] + (m1 - cima[1]) * t))
        a[y, :] = 0
        for x in range(x0, x1 + 1):
            u = (x - x0) / max(1, x1 - x0)
            a[y, x] = modelo[int(round(m0 + u * (m1 - m0)))]
        # contorno escuro nas laterais
        a[y, x0, :3] = (24, 20, 22)
        a[y, x1, :3] = (24, 20, 22)
    # a alça da pasta que sobrava entre os dedos
    for y in range(127, PASTA_Y0):
        for x in range(a.shape[1]):
            r, g, b, al = a[y, x]
            if al > 128 and x < 13:
                a[y, x] = 0
    return a


def tomba_cabeca(a):
    """Cabeça caída para frente (o corpo olha para a direita): desliza as linhas de cima para a direita e um pouco
    para baixo, como se o pescoço dobrasse."""
    a = a.copy()
    cab = a[:CABECA_Y1 + 1].copy()
    a[:CABECA_Y1 + 1] = 0
    for y in range(CABECA_Y1 + 1):
        dx = int(round((CABECA_Y1 - y) * 0.13))
        ny = y + 2
        row = np.roll(cab[y], dx, axis=0)
        if dx:
            row[:dx] = 0
        if ny <= CABECA_Y1 + 2:
            m = row[:, 3] > 0
            a[ny][m] = row[m]
    return a


def pes_para_baixo(a):
    """Os sapatos giram com a ponta para baixo (o corpo pendurado não apoia os pés). O giro é em volta do
    tornozelo, então o sapato continua preso na barra da calça."""
    a = a.copy()
    h, w = a.shape[:2]
    sap = a[SAPATO_Y0:].copy()
    a[SAPATO_Y0:] = 0
    tornozelo = (TORNOZELO_X, 0)
    T = 64
    tela = np.zeros((T, T, 4), np.uint8)
    tela[T // 2:T // 2 + sap.shape[0], T // 2 - tornozelo[0]:T // 2 - tornozelo[0] + sap.shape[1]] = sap
    g = np.array(Image.fromarray(tela, "RGBA").rotate(-PE_GIRO, resample=Image.NEAREST, center=(T // 2, T // 2)))
    ys, xs = np.nonzero(g[..., 3] > 0)
    out = np.zeros((max(h, SAPATO_Y0 + ys.max() - T // 2 + 1), w + 6, 4), np.uint8)
    out[:h, :w] = a
    for y, x in zip(ys, xs):
        oy, ox = SAPATO_Y0 + y - T // 2, tornozelo[0] + x - T // 2
        if oy >= SAPATO_Y0 and 0 <= ox < out.shape[1]:
            out[oy, ox] = g[y, x]
    return out


def repinta(a):
    out = a.copy().astype(float)
    hh, ww = a.shape[:2]
    for y in range(hh):
        for x in range(ww):
            if a[y, x, 3] == 0:
                continue
            h, l, s = hls(a[y, x, :3].astype(float))
            r, g, b = a[y, x, :3].astype(int)
            pele = r > 120 and r - b > 18
            if y <= CABECA_Y1 + 2 and not pele:
                # cabelo: grisalho, mantendo os fios escuros
                out[y, x, :3] = cor(0.08, 0.14 + l * 1.25, 0.03)
            elif pele:
                # pele: pálida, fria, sem sangue
                out[y, x, :3] = cor(0.12, min(0.74, l * 0.95), 0.07) * np.array([0.92, 0.95, 1.0])
    return out.clip(0, 255).astype(np.uint8)


def corda_px(img, x, y, i):
    """Um pixel de corda com a torção desenhada (luz a cada 3 px)."""
    if not (0 <= y < img.shape[0] and 0 <= x < img.shape[1]):
        return
    if img[y, x, 3] > 0:
        return  # a corda passa por trás do corpo
    c = CORDA[2] if i % 3 == 0 else CORDA[1]
    img[y, x] = (*c, 255)


def main():
    im = Image.open(ORIGEM).convert("RGBA")
    a = np.array(im.crop(im.getbbox()))
    a = tira_pasta(a)
    a = tomba_cabeca(a)
    a = pes_para_baixo(a)
    a = repinta(a)
    ch, cw = a.shape[:2]

    # tela: do topo do cano até os pés
    pes_y = CHAO - PES_DO_CHAO
    topo_corpo = pes_y - ch
    H = pes_y - CANO_TOPO + 1
    # a corda fica exatamente no meio da imagem (o pivô do sprite é o meio do topo)
    ox, oy = cw - 11, topo_corpo - CANO_TOPO
    W = 2 * (ox + 15)
    tela = np.zeros((H, W, 4), np.uint8)
    reg = tela[oy:oy + ch, ox:ox + cw]
    m = a[..., 3] > 0
    reg[m] = a[m]

    # o nó atrás da nuca e a corda subindo reta até o cano
    no_x = ox + 15
    no_y = oy + PESCOCO_Y - 2
    for y in range(0, no_y + 1):
        corda_px(tela, no_x, y, y)
        if tela[y, no_x + 1, 3] == 0:
            tela[y, no_x + 1] = (*CORDA[0], 255)      # lado da sombra
    # amarração no cano: umas voltas de corda bem embaixo dele
    for y in range(0, CANO_BAIXO - CANO_TOPO + 1):
        for xx in range(no_x - 3, no_x + 5):
            tela[y, xx] = (*CORDA[2 if (xx + y) % 3 == 0 else (0 if y == CANO_BAIXO - CANO_TOPO else 1)], 255)
    # o nó (um bolo de corda) e o laço em volta do pescoço, por cima do corpo
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            if abs(dx) + abs(dy) <= 3:
                tela[no_y + dy, no_x + dx] = (*CORDA[0 if (dx + dy) % 2 else 1], 255)
    tela[no_y - 1, no_x] = (*CORDA[2], 255)
    py = oy + PESCOCO_Y
    linha = np.nonzero(tela[py, :, 3] > 0)[0]
    for x in range(no_x, linha.max() + 1):
        if tela[py, x, 3] > 0:
            tela[py, x] = (*CORDA[1 if x % 3 else 2], 255)
            tela[py + 1, x] = (*CORDA[0], 255)

    Image.fromarray(tela, "RGBA").save(os.path.join(SAIDA, "enforcado.png"))
    print(f"enforcado {W}x{H}: pivô no topo, corda em x={no_x} (meio da imagem em {W / 2:.1f}); corpo de {ch}px; "
          f"pés {PES_DO_CHAO}px acima do chão")

    Image.fromarray(cadeira(), "RGBA").save(os.path.join(SAIDA, "cadeira_caida.png"))
    Image.fromarray(poca(64, 14), "RGBA").save(os.path.join(SAIDA, "sangue_chefe.png"))


def cadeira():
    """Cadeira de madeira chutada: desenhada de perfil, em pé, e depois tombada para trás (o encosto no chão,
    as pernas para cima e para a direita)."""
    # proporção de verdade para um corpo de ~234 px: encosto de ~100 px, assento na altura dos pés (52 px)
    w, h = 50, 100
    assento = h - PES_DO_CHAO
    s = np.zeros((h, w, 4), np.uint8)
    esc, mei, luz = (26, 18, 12), (60, 44, 30), (92, 69, 46)
    def ret(x0, y0, x1, y1, c):
        s[y0:y1 + 1, x0:x1 + 1] = (*c, 255)
    def tabua(x0, y0, x1, y1):           # peça de madeira: contorno escuro, miolo, fio de luz em cima/à esquerda
        ret(x0, y0, x1, y1, esc)
        if x1 - x0 >= 2 and y1 - y0 >= 2:
            ret(x0 + 1, y0 + 1, x1 - 1, y1 - 1, mei)
            if x1 - x0 > y1 - y0:
                s[y0 + 1, x0 + 1:x1] = (*luz, 255)
            else:
                s[y0 + 1:y1, x0 + 1] = (*luz, 255)
    tabua(0, 0, 6, h - 1)                # montante do encosto, que vira a perna de trás
    tabua(0, 0, 14, 6)                   # topo do encosto
    for y in (16, 28, 40):               # ripas do encosto
        tabua(5, y, 10, y + 4)
    tabua(0, assento - 3, w - 1, assento + 4)   # assento
    tabua(w - 7, assento + 4, w - 1, h - 1)     # perna da frente
    tabua(6, assento + 30, w - 7, assento + 34) # travessa das pernas
    g = Image.fromarray(s, "RGBA").rotate(90, resample=Image.NEAREST, expand=True)
    g = g.crop(g.getbbox())
    a = np.array(g)
    out = np.zeros((a.shape[0] + 1, a.shape[1] + 2, 4), np.uint8)
    out[:a.shape[0], 1:1 + a.shape[1]] = a
    out[-1, 0:a.shape[1]] = (10, 8, 6, 200)                                        # sombra no chão
    return out


def poca(w, h):
    """Poça de sangue embaixo dos pés, irregular, com reflexo do molhado."""
    rng = np.random.default_rng(5)
    yy, xx = np.mgrid[0:h, 0:w]
    m = np.zeros((h, w), bool)
    for cx, rx, ry in ((0.5, 0.34, 0.5), (0.3, 0.2, 0.38), (0.72, 0.2, 0.4), (0.9, 0.08, 0.28)):
        m |= ((xx - w * cx) / (w * rx)) ** 2 + ((yy - h * 0.5) / (h * ry)) ** 2 < 1
    borda = m & ~(np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1))
    s = np.zeros((h, w, 4), np.uint8)
    s[m] = (62, 8, 8, 240)
    d = 1 - (((xx - w * 0.5) / (w * 0.34)) ** 2 + ((yy - h * 0.5) / (h * 0.5)) ** 2)
    s[m & (d > 0.4)] = (48, 5, 5, 245)
    s[borda] = (86, 14, 12, 230)
    for _ in range(8):
        x = int(rng.uniform(0.25, 0.8) * w); y = int(rng.uniform(0.25, 0.45) * h)
        if m[y, x]:
            s[y, x] = (112, 26, 22, 235)
    return s


if __name__ == "__main__":
    main()
