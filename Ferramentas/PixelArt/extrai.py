"""Transforma uma arte "estilo pixel" (blocos irregulares, halo, muitas cores) em sprite de pixels reais.

python extrai.py <referencia.png> <id> <altura> [altura ...]

A altura precisa ficar perto da grade nativa da arte (cerca de 7,5 px por bloco): reduções maiores destroem o
rosto, seja qual for o método (testados centro, média de área e cor predominante). Depois limpa o ruído e
aplica retoques.py. Pés sempre na última linha do PNG.
"""
import os
import sys

import numpy as np
from PIL import Image

import retoques
import sprite

CORES = 24        # por personagem; a mesma paleta vale para todos os quadros dele
LUM = np.array([0.3, 0.59, 0.11])


def fora(corpo):
    """Tudo que não é corpo e se conecta à borda da imagem."""
    h, w = corpo.shape
    visto = np.zeros((h, w), bool)
    borda = [(0, x) for x in range(w)] + [(h - 1, x) for x in range(w)] + [(y, 0) for y in range(h)] + [(y, w - 1) for y in range(h)]
    pilha = [p for p in borda if not corpo[p]]
    for p in pilha:
        visto[p] = True
    while pilha:
        y, x = pilha.pop()
        for j, i in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= j < h and 0 <= i < w and not visto[j, i] and not corpo[j, i]:
                visto[j, i] = True
                pilha.append((j, i))
    return visto


def carrega(path):
    im = Image.open(path).convert("RGBA")
    a = np.array(im)
    alpha = a[:, :, 3].astype(float)
    rgb = a[:, :, :3].astype(float)
    lum = rgb @ LUM
    corpo = (alpha > 150) & (lum < 240)
    # a remoção de fundo do gerador abre buracos em partes escuras: só é fundo o que toca a borda da imagem
    solido = corpo
    corpo = ~fora(corpo)
    ys, xs = np.nonzero(corpo)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    return rgb[y0:y1, x0:x1], corpo[y0:y1, x0:x1], solido[y0:y1, x0:x1]


def paleta(px, n):
    """K-means começando por faixas de luminosidade; cada cor final é a mediana do grupo, uma cor que existe na arte."""
    rng = np.random.default_rng(1)
    px = px[rng.choice(len(px), min(60000, len(px)), replace=False)]
    lum = px @ LUM
    lim = np.quantile(lum, np.linspace(0, 1, n + 1))
    c = np.array([px[(lum >= lim[i]) & (lum <= lim[i + 1])].mean(0) for i in range(n)])
    for _ in range(30):
        rot = ((px[:, None, :] - c[None, :, :]) ** 2).sum(2).argmin(1)
        for i in range(n):
            if (rot == i).any():
                c[i] = np.median(px[rot == i], axis=0)
    return np.round(c).astype(int)


def indexa(rgb, pal):
    out = np.zeros(rgb.shape[:2], np.int32)
    for y in range(0, rgb.shape[0], 64):
        bloco = rgb[y:y + 64]
        out[y:y + 64] = ((bloco[:, :, None, :] - pal[None, None, :, :]) ** 2).sum(3).argmin(2)
    return out


def reduz(rgb, corpo, solido, pal, altura):
    """Média de cada célula, perto da grade nativa (reduções maiores destroem o rosto)."""
    H, W = corpo.shape
    bloco = H / altura
    largura = int(round(W / bloco))
    cor = np.zeros((altura, largura, 3))
    cheio = np.zeros((altura, largura), bool)
    tem = np.zeros((altura, largura), bool)
    for y in range(altura):
        y0, y1 = int(y * bloco), max(int((y + 1) * bloco), int(y * bloco) + 1)
        my = (y1 - y0) // 4
        for x in range(largura):
            x0, x1 = int(x * bloco), max(int((x + 1) * bloco), int(x * bloco) + 1)
            mx = (x1 - x0) // 4
            m = corpo[y0 + my:y1 - my, x0 + mx:x1 - mx]
            if m.size and m.mean() >= 0.45:
                cheio[y, x] = True
                ok = solido[y0:y1, x0:x1]
                if ok.any():  # célula inteira: a mediana do miolo às vezes pega só o tom mais escuro da dobra
                    cor[y, x] = rgb[y0:y1, x0:x1][ok].mean(0)
                    tem[y, x] = True
    # células só de buraco herdam dos vizinhos
    while True:
        falta = np.argwhere(cheio & ~tem)
        if not len(falta):
            break
        novos = []
        for y, x in falta:
            v = [cor[j, i] for j, i in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1))
                 if 0 <= j < altura and 0 <= i < largura and tem[j, i]]
            if v:
                novos.append((y, x, np.median(v, axis=0)))
        if not novos:
            break
        for y, x, c in novos:
            cor[y, x] = c
            tem[y, x] = True
    return indexa(cor, pal), cheio


def limpa(idx, cheio, pal, passadas=2):
    """Tira pixels soltos da silhueta e pontos de cor sem vizinho igual, se o tom for próximo do entorno."""
    h, w = idx.shape
    idx, cheio = idx.copy(), cheio.copy()
    viz = [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]
    for y in range(h):
        for x in range(w):
            if cheio[y, x] and not any(0 <= y + j < h and 0 <= x + i < w and cheio[y + j, x + i] for j, i in viz):
                cheio[y, x] = False
    lum = pal @ LUM
    for _ in range(passadas):
        novo = idx.copy()
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                if not cheio[y, x]:
                    continue
                nb = [idx[y + j, x + i] for j, i in viz if cheio[y + j, x + i]]
                if len(nb) < 5:
                    continue
                if nb.count(idx[y, x]) <= 1:
                    alvo = max(set(nb), key=nb.count)
                    if abs(lum[alvo] - lum[idx[y, x]]) < 30:
                        novo[y, x] = alvo
        idx = novo
    return idx, cheio


def main():
    ref, id_ = sys.argv[1], sys.argv[2]
    alturas = [int(a) for a in sys.argv[3:]]
    rgb, corpo, solido = carrega(ref)
    pal = paleta(rgb[solido], CORES)
    print("recorte", corpo.shape[::-1], "cores", len(pal))
    pasta = os.path.join(sprite.ASSETS, id_.capitalize())
    os.makedirs(pasta, exist_ok=True)
    for h in alturas:
        red, cheio = reduz(rgb, corpo, solido, pal, h)
        red, cheio = limpa(red, cheio, pal)
        w = cheio.shape[1]
        im = np.zeros((h, w, 4), np.uint8)
        im[:, :, :3] = pal[red]
        im[:, :, 3] = cheio * 255
        img = Image.fromarray(im, "RGBA").copy()
        retoques.aplica(img, id_, h)
        nome = f"{id_}_base_{h}.png"
        img.save(os.path.join(pasta, nome))
        c = sprite.cheques(img)
        print(nome, img.size, c["cores"], "cores,", len(c["orfaos"]), "órfãos")


if __name__ == "__main__":
    main()
