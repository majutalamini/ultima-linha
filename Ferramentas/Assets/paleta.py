"""Paleta oficial de Última Linha: as cores dos cenários que já estão bons (a estação do fim do C2 e o vagão do C0/C1).
Grava Ferramentas/Assets/paleta_oficial.png (amostras) e serve de módulo: prende(rgb) leva qualquer imagem para a paleta.
Uso: python Ferramentas/Assets/paleta.py"""
import os

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
FONTES = [os.path.join(RAIZ, "Assets", "Art", "Cenario2", "Objetos", "estacao.png"),
          os.path.join(RAIZ, "Assets", "Art", "Cenario0", "vagao_antigo.png")]
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 - 0.5


def cores():
    """As cores únicas das imagens de referência, ordenadas por brilho."""
    todas = np.concatenate([np.array(Image.open(f).convert("RGB")).reshape(-1, 3) for f in FONTES])
    u = np.unique(todas, axis=0)
    return u[np.argsort(u.astype(int).sum(1))]


def prende(rgb, pal=None, dither=0.0):
    """Cada pixel vai para a cor mais próxima da paleta. dither (0-1): Bayer 4x4 suave, para degradês não virarem faixas."""
    pal = cores() if pal is None else pal
    a = rgb[..., :3].astype(np.float32)
    if dither > 0:
        h, w = a.shape[:2]
        a = a + np.tile(BAYER, (h // 4 + 1, w // 4 + 1))[:h, :w, None] * dither * 24.0
    p = pal.astype(np.float32)
    out = np.empty(a.shape[:2] + (3,), np.uint8)
    plano = a.reshape(-1, 3)
    saida = out.reshape(-1, 3)
    for i in range(0, len(plano), 65536):  # em blocos, para não estourar a memória
        bloco = plano[i:i + 65536]
        d = ((bloco[:, None, :] - p[None, :, :]) ** 2).sum(2)
        saida[i:i + 65536] = pal[d.argmin(1)]
    return out


if __name__ == "__main__":
    pal = cores()
    lado = 24
    img = Image.new("RGB", (lado * 16, lado * ((len(pal) + 15) // 16)), (0, 0, 0))
    for i, c in enumerate(pal):
        img.paste(tuple(int(v) for v in c), ((i % 16) * lado, (i // 16) * lado, (i % 16 + 1) * lado, (i // 16 + 1) * lado))
    img.save(os.path.join(AQUI, "paleta_oficial.png"))
    teste = prende(np.array([[[255, 0, 0], [0, 0, 0]]], np.uint8), pal)
    assert teste.shape == (1, 2, 3) and tuple(teste[0, 1]) == tuple(pal[0])
    print(len(pal), "cores ->", os.path.join(AQUI, "paleta_oficial.png"))
