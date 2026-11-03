"""Converte grades de texto em PNG de pixel art e gera as prévias ampliadas.

Cada caractere da grade é um pixel; '.' é transparente. Sem filtro, sem antialiasing.
"""
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ASSETS = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt")
PREVIA = os.path.join(AQUI, "previa")


def rgba(hexa):
    h = hexa.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


def grade(linhas, paleta, w, h):
    assert len(linhas) == h, f"{len(linhas)} linhas, esperava {h}"
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()
    for y, ln in enumerate(linhas):
        ln = ln.rstrip(".")
        assert len(ln) <= w, f"linha {y} com {len(ln)} colunas"
        for x, c in enumerate(ln):
            if c != ".":
                px[x, y] = rgba(paleta[c])
    return img


def cheques(img):
    """Problemas objetivos: pixels sem vizinho e altura/largura usadas. Não substitui olhar o PNG."""
    w, h = img.size
    a = img.getchannel("A").load()
    orfaos = []
    for y in range(h):
        for x in range(w):
            if a[x, y] and not any(0 <= x + i < w and 0 <= y + j < h and a[x + i, y + j]
                                   for i in (-1, 0, 1) for j in (-1, 0, 1) if i or j):
                orfaos.append((x, y))
    cores = {c for _, c in img.getcolors(w * h) if c[3]}
    return dict(bbox=img.getbbox(), cores=len(cores), orfaos=orfaos)


def amplia(img, k, fundo=None):
    grande = img.resize((img.width * k, img.height * k), Image.NEAREST)
    if fundo is None:
        return grande
    f = Image.new("RGBA", grande.size, fundo + (255,))
    f.alpha_composite(grande)
    return f
