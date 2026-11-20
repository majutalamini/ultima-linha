"""Prepara os efeitos visuais do jogo em Assets/Art/Efeitos (grade de 58 px, sem reescala: os quadros de 32 px dos pacotes
já ficam do tamanho certo ao lado do Kai).
- Faísca e respingo: quadros dos pacotes da IceMaan (Ferramentas/Assets/originais, fora do git; ver LICENCAS.md),
  recortados da folha e recoloridos (faísca: branco-amarelado de lâmpada; respingo: cinza frio da água suja).
- Chiado de monitor e silhueta da sombra: desenhados aqui.
Uso: python Ferramentas/Assets/trata.py   (sem os pacotes baixados, só gera o que é desenhado aqui)"""
import os

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ORIG = os.path.join(AQUI, "originais")
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Efeitos")
FOLHAS = {  # nome no jogo: (arquivo da folha, cor (r, g, b) que substitui o matiz original)
    "faisca": ("icemaan_pixel_vfx_essentials/extraido/Spritesheets/Lightning/Spark Burst.png", (236, 226, 190)),
    "respingo": ("icemaan_pixel_vfx_essentials/extraido/Spritesheets/Ambient/Puddle Splash.png", (120, 128, 132)),
}


def quadros(folha, lado=32):
    a = np.array(Image.open(folha).convert("RGBA"))
    return [a[:, i:i + lado] for i in range(0, a.shape[1], lado)]


def recolore(q, cor):
    """Mantém o brilho e a transparência de cada pixel, troca o matiz (azul nos originais) pela cor dada."""
    out = q.copy()
    lum = q[..., :3].astype(float).max(2, keepdims=True) / 255.0
    out[..., :3] = np.clip(lum * np.array(cor, float), 0, 255).astype(np.uint8)
    return out


def chiado(n=4, w=40, h=28, semente=7):
    """Tela de monitor de câmera chiando: ruído frio, linhas de varredura e uma faixa clara que desce."""
    rng = np.random.default_rng(semente)
    out = []
    for i in range(n):
        v = rng.integers(20, 110, (h, w)).astype(float)
        v[::2] *= 0.7
        faixa = (i * h // n) % h
        v[faixa:faixa + 3] += 60
        rgb = np.dstack([v * 0.75, v * 0.9, v * 1.0]).clip(0, 255)
        out.append(np.dstack([rgb, np.full((h, w), 200)]).astype(np.uint8))
    return out


def sombra(w=60, h=200):
    """Silhueta de pessoa de lado, só a mancha (a cor e a opacidade vêm do jogo)."""
    a = np.zeros((h, w, 4), np.uint8)
    yy, xx = np.mgrid[0:h, 0:w]
    cabeca = (xx - 30) ** 2 + (yy - 18) ** 2 < 15 ** 2
    corpo = (abs(xx - 30) < 16 - (yy - 40) * 0.02) & (yy > 32) & (yy < 125)
    pernas = ((abs(xx - 24) < 7) | (abs(xx - 36) < 7)) & (yy >= 120)
    m = cabeca | corpo | pernas
    a[m] = (255, 255, 255, 255)
    return a


def salva(img, nome):
    Image.fromarray(img, "RGBA").save(os.path.join(SAIDA, nome + ".png"))


def main():
    os.makedirs(SAIDA, exist_ok=True)
    for nome, (arq, cor) in FOLHAS.items():
        caminho = os.path.join(ORIG, arq)
        if not os.path.exists(caminho):
            print("faltando (baixe o pacote, ver LICENCAS.md):", arq)
            continue
        qs = [recolore(q, cor) for q in quadros(caminho)]
        for i, q in enumerate(qs):
            salva(q, f"{nome}_{i}")
        print(nome, len(qs), "quadros")
    for i, q in enumerate(chiado()):
        salva(q, f"chiado_{i}")
    s = sombra()
    assert s[..., 3].any()
    salva(s, "sombra_pessoa")
    print("->", SAIDA)


if __name__ == "__main__":
    main()
