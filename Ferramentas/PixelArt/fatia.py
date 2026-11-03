"""Corta as folhas de animação (fundo verde) em quadros de pixel art.

python fatia.py <pasta das folhas> <kai|souta|ren|medir>

Cada folha: separa as poses por componentes conexos, reduz para a altura do personagem, aplica uma paleta única
do personagem e grava {clipe}_{nn}.png num quadro 256x256 com os pés na última linha e a âncora no centro.
"""
import os
import sys

import numpy as np
from PIL import Image

import extrai
import sprite

QUADRO = 256
CORES = 28
SATURACAO = 0.45

ALTURA = {"kai": 222, "souta": 232, "ren": 215}

# arquivo, clipe, nº de poses, âncora horizontal, altura de pé dentro da folha (None = a maior pose da folha)
# âncora: "pes" para quem fica parado no lugar, "tronco" para quem desloca as pernas,
# "costas" para poses em que o braço puxa a média para a frente
FOLHAS = {
    "kai": [
        ("kai 1", "idle", 6, "pes", None), ("kai caminhada", "walk", 6, "tronco", None),
        ("kai correndo", "run", 6, "tronco", 640), ("kai pulando", "jump", 6, "tronco", 600),
        ("kai falando", "talk", 4, "pes", None), ("kai alavanca", "lever", 6, "costas", None),
        ("kai vasculhando", "search", 6, "costas", None), ("kai suspirando", "act", 6, "pes", None),
        ("kai final", "final", 6, "costas", None),
    ],
    "souta": [
        ("souta parado", "idle", 6, "pes", None), ("souta andando", "walk", 6, "tronco", None),
        ("souta falando", "talk", 4, "pes", None), ("souta relogio", "act", 6, "pes", None),
        ("souta arma", "gun", 6, "costas", None), ("souta caido", "fallen", 6, "costas", 530),
    ],
    "ren": [
        ("ren parado", "idle", 6, "pes", None), ("ren caminhada", "walk", 6, "tronco", None),
        ("ren falando", "talk", 4, "pes", None), ("ren por cima do ombro", "act", 6, "pes", None),
        ("ren panico", "panic", 6, "costas", None),
    ],
}


def mascara(rgb):
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return ~((g.astype(int) - np.maximum(r, b).astype(int)) > 55)


def componentes(m, passo=3):
    """Componentes conexos numa versão reduzida da máscara: rótulos e [x0, y0, x1, y1, tamanho, rótulo] em pixels da folha."""
    p = m[::passo, ::passo]
    h, w = p.shape
    rot = np.zeros((h, w), np.int32)
    caixas, atual = [], 0
    for y in range(h):
        for x in range(w):
            if p[y, x] and not rot[y, x]:
                atual += 1
                pilha, rot[y, x] = [(y, x)], atual
                x0, y0, x1, y1, n = x, y, x, y, 0
                while pilha:
                    cy, cx = pilha.pop()
                    n += 1
                    x0, y0, x1, y1 = min(x0, cx), min(y0, cy), max(x1, cx), max(y1, cy)
                    for j in range(max(cy - 1, 0), min(cy + 2, h)):
                        for i in range(max(cx - 1, 0), min(cx + 2, w)):
                            if p[j, i] and not rot[j, i]:
                                rot[j, i] = atual
                                pilha.append((j, i))
                caixas.append([x0 * passo, y0 * passo, (x1 + 1) * passo, (y1 + 1) * passo, n, atual])
    return rot, caixas


def poses(m, n, passo=3):
    """Os n maiores componentes, da esquerda para a direita, cada um com sua própria máscara: braços que invadem o
    retângulo da pose vizinha ficam de fora. Fios soltos vão para a pose mais próxima."""
    rot, cs = componentes(m, passo)
    cs.sort(key=lambda c: -c[4])
    grandes = sorted(cs[:n], key=lambda c: c[0])
    rotulos = [[c[5]] for c in grandes]
    for c in cs[n:]:
        if c[4] < 3:
            continue
        cx = (c[0] + c[2]) / 2
        i = min(range(n), key=lambda k: 0 if grandes[k][0] <= cx <= grandes[k][2] else min(abs(cx - grandes[k][0]), abs(cx - grandes[k][2])))
        g = grandes[i]
        g[0], g[1], g[2], g[3] = min(g[0], c[0]), min(g[1], c[1]), max(g[2], c[2]), max(g[3], c[3])
        rotulos[i].append(c[5])
    saida = []
    h, w = m.shape
    for g, r in zip(grandes, rotulos):
        sel = np.isin(rot, r)
        sel = sel | np.roll(sel, 1, 0) | np.roll(sel, -1, 0) | np.roll(sel, 1, 1) | np.roll(sel, -1, 1)
        grande = np.repeat(np.repeat(sel, passo, 0), passo, 1)[:h, :w]
        saida.append((g[0], g[1], g[2], g[3], m & grande))
    return saida


def ancora(cor, cheio, tipo):
    """Coluna do corpo que fica parada de um quadro para o outro."""
    ys = np.nonzero(cheio.any(1))[0]
    topo, base = ys.min(), ys.max()
    if tipo == "pes":
        return int(np.percentile(np.nonzero(cheio[base - 14:base + 1])[1], 25))
    faixa = cheio[topo + int((base - topo) * 0.22):topo + int((base - topo) * 0.42) + 1]
    colunas = np.nonzero(faixa)[1]
    return int(np.median(colunas)) if tipo == "tronco" else int(np.percentile(colunas, 12)) + 12


def reduz(rgb, m, escala):
    alt, larg = rgb.shape[:2]
    h, w = max(1, round(alt * escala)), max(1, round(larg * escala))
    pre = Image.fromarray((rgb * m[:, :, None]).astype(np.uint8), "RGB").resize((w, h), Image.BOX)
    cob = np.array(Image.fromarray((m * 255).astype(np.uint8), "L").resize((w, h), Image.BOX)).astype(float) / 255
    cor = np.array(pre).astype(float) / np.maximum(cob[:, :, None], 0.02)
    return np.clip(cor, 0, 255), cob >= 0.5


def neutro(pal):
    """As folhas vieram mais avermelhadas que as artes-base: tira parte da saturação."""
    lum = (pal @ extrai.LUM)[:, None]
    return np.clip(np.round(lum + (pal - lum) * SATURACAO), 0, 255).astype(int)


def processa(pasta, id_):
    alvo = ALTURA[id_]
    fs = FOLHAS[id_]
    amostras = []
    dados = []
    for arq, clipe, n, anc, altura in fs:
        rgb = np.array(Image.open(os.path.join(pasta, id_, arq + ".png")).convert("RGB"))
        m = mascara(rgb)
        ps = poses(m, n)
        amostras.append(rgb[m][::40])
        dados.append((rgb, m, ps, clipe, anc, altura))
    pal = neutro(extrai.paleta(np.concatenate(amostras).astype(float), CORES))
    destino = os.path.join(sprite.ASSETS, id_.capitalize())
    for rgb, m, ps, clipe, anc, altura in dados:
        # a primeira pose é a neutra: usar a mais alta faria o personagem encolher ao entrar no gesto
        h_pe = altura or ps[0][3] - ps[0][1]
        esc = alvo / h_pe
        ajuste = 0  # alinha os pés da primeira pose com os do personagem parado: o clipe não salta ao começar
        for i, (x0, y0, x1, y1, mp) in enumerate(ps):
            cor, cheio = reduz(rgb[y0:y1, x0:x1], mp[y0:y1, x0:x1], esc)
            if i == 0 and clipe != "walk" and clipe != "run" and clipe != "jump" and clipe != "fallen":
                ajuste = ancora(cor, cheio, anc) - ancora(cor, cheio, "pes")
            idx = extrai.indexa(cor, pal)
            idx, cheio = extrai.limpa(idx, cheio, pal, passadas=1)
            cx = ancora(cor, cheio, anc) - ajuste
            h, w = cheio.shape
            q = np.zeros((QUADRO, QUADRO, 4), np.uint8)
            ox, oy = QUADRO // 2 - cx, QUADRO - h
            xs0, xs1 = max(0, -ox), min(w, QUADRO - ox)
            ys0 = max(0, -oy)
            q[oy + ys0:oy + h, ox + xs0:ox + xs1, :3] = pal[idx[ys0:, xs0:xs1]]
            q[oy + ys0:oy + h, ox + xs0:ox + xs1, 3] = cheio[ys0:, xs0:xs1] * 255
            Image.fromarray(q, "RGBA").save(os.path.join(destino, f"{clipe}_{i:02d}.png"))
    return pal


def medir(pasta):
    for id_, fs in FOLHAS.items():
        for arq, clipe, n, _a, _h in fs:
            rgb = np.array(Image.open(os.path.join(pasta, id_, arq + ".png")).convert("RGB"))
            ps = poses(mascara(rgb), n)
            print(id_, clipe, len(ps), "alturas", [p[3] - p[1] for p in ps], "larguras", [p[2] - p[0] for p in ps], "fundo", [p[3] for p in ps])


def main():
    pasta, id_ = sys.argv[1], sys.argv[2]
    if id_ == "medir":
        medir(pasta)
    else:
        processa(pasta, id_)


if __name__ == "__main__":
    main()
