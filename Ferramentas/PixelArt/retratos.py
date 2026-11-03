"""Monta os retratos do diálogo a partir das ilustrações com fundo verde.

python retratos.py   (lê referencias/retratos/{id}.png, grava Assets/Art/Cenario1/Retratos/{id}_retrato.png)

Mesmo tamanho dos anteriores (620x1024): as cenas apontam para esses arquivos, nada precisa ser reconstruído.
"""
import os

import numpy as np
from PIL import Image, ImageFilter

import sprite

W, H = 620, 1024
ESCALA = 0.667
TOPO = 110  # margem acima do cabelo, para a cabeça ficar sobre a linha do painel
VERMELHO = np.array([235, 28, 22], dtype=np.float32)

# id: centro da cabeça na imagem original, espelhar (todos olham para a direita, em direção ao texto)
FICHAS = {"kai": (540, False), "souta": (530, False), "ren": (520, True)}


def suave(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def recorta(im):
    """Alfa pela dominância do verde, e tira o verde refletido nos fios semitransparentes."""
    a = np.asarray(im.convert("RGB"), dtype=np.float32)
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    dom = g - np.maximum(r, b)
    alfa = 1.0 - suave(14.0, 70.0, dom)
    a[:, :, 1] = np.minimum(g, np.maximum(r, b) * 1.02)
    return np.dstack([a, alfa * 255]).astype(np.uint8)


def enquadra(rgba, centro, espelhar):
    im = Image.fromarray(rgba, "RGBA")
    if espelhar:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
        centro = im.width - centro
    g = im.resize((round(im.width * ESCALA), round(im.height * ESCALA)), Image.LANCZOS)
    q = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    q.paste(g, (W // 2 - round(centro * ESCALA) - 10, TOPO))
    return q


def clima(im):
    """Luz vermelha vindo da esquerda, brilho atrás do personagem e fade na base."""
    a = np.asarray(im.getchannel("A"), dtype=np.float32) / 255.0
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    xs = np.linspace(0, 1, W, dtype=np.float32)[None, :, None]
    lavagem = (1.0 - suave(0.0, 0.8, xs)) * 0.20
    rgb = rgb * np.array([0.86, 0.84, 0.88], dtype=np.float32)
    rgb = rgb * (1 - lavagem) + VERMELHO * 0.5 * lavagem
    rgb *= 1.0 - 0.22 * suave(0.5, 1.0, xs)

    desloc = np.zeros_like(a)
    desloc[:, 6:] = a[:, :-6]
    desloc[:, :6] = a[:, :6]  # a borda da imagem corta o ombro: ali não é contorno
    aro = a * (1.0 - desloc)
    aro = Image.fromarray((np.clip(aro, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.3))
    aro = np.clip(np.asarray(aro, dtype=np.float32)[..., None] / 255.0 * 1.2, 0, 1) * 0.8
    rgb = rgb * (1 - aro) + VERMELHO * aro

    ys = np.linspace(0, 1, H, dtype=np.float32)[:, None]
    fade = (1.0 - suave(0.64, 0.97, ys)) * suave(0.0, 0.13, xs[:, :, 0]) * (1.0 - suave(0.8, 1.0, xs[:, :, 0]))
    corpo = a * fade
    brilho = Image.fromarray((a * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(40))
    brilho = np.asarray(brilho, dtype=np.float32) / 255.0
    brilho = np.clip(brilho * (1.0 - suave(0.1, 0.95, xs[:, :, 0])) * fade * 0.8, 0, 0.5)

    sai_a = corpo + brilho * (1 - corpo)
    sai = (rgb * corpo[..., None] + VERMELHO * 0.8 * (brilho * (1 - corpo))[..., None]) / np.maximum(sai_a[..., None], 1e-5)
    return Image.fromarray(np.dstack([np.clip(sai, 0, 255), np.clip(sai_a * 255, 0, 255)]).astype(np.uint8), "RGBA")


def main():
    destino = os.path.join(sprite.RAIZ, "Assets", "Art", "Cenario1", "Retratos")
    painel = Image.new("RGBA", (1920, 512), (6, 6, 8, 255))
    for i, (id_, (centro, espelhar)) in enumerate(FICHAS.items()):
        fonte = Image.open(os.path.join(sprite.AQUI, "referencias", "retratos", id_ + ".png"))
        q = clima(enquadra(recorta(fonte), centro, espelhar))
        q.save(os.path.join(destino, f"{id_}_retrato.png"))
        painel.alpha_composite(q.resize((310, 512), Image.LANCZOS), (20 + i * 640, 0))
    painel.convert("RGB").save(os.path.join(sprite.PREVIA, "retratos_painel.png"))


if __name__ == "__main__":
    main()
