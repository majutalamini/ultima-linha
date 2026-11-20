"""O corpo do chefe (C5) a partir de um passageiro do C0 (pe_8, o homem de sobretudo), deitado de costas com a cabeça à
direita, na grade de 58 px. A cabeça vira um sprite à parte (ela se solta quando o Kai atravessa o corpo) e uma poça de
sangue vai por baixo. Grava em Assets/Art/Cenarios4a7: corpo_chefe.png, cabeca_chefe.png, sangue_chefe.png.
Uso: python Ferramentas/Cenario5/corpo.py"""
import os

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ORIGEM = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt", "Passageiros", "pe_8.png")
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Cenarios4a7")
CABECA = 40  # px do alto da figura que são a cabeça


def main():
    im = Image.open(ORIGEM).convert("RGBA")
    im = im.crop(im.getbbox())
    a = np.array(im).astype(float)
    a[..., :3] = a[..., :3] * np.array([0.8, 0.8, 0.86])  # sem cor de vida, um pouco mais frio
    deitado = np.rot90(a.clip(0, 255).astype(np.uint8), k=-1)  # o alto da figura vai para a direita
    h, w = deitado.shape[:2]
    corpo, cabeca = deitado[:, :w - CABECA].copy(), deitado[:, w - CABECA:].copy()
    for nome, img in (("corpo_chefe", corpo), ("cabeca_chefe", cabeca)):
        ys = np.nonzero(img[..., 3].max(1))[0]
        img = img[ys[0]:ys[-1] + 1]
        assert img[..., 3].any()
        Image.fromarray(img, "RGBA").save(os.path.join(SAIDA, nome + ".png"))
    sw, sh = w + 70, 12
    yy, xx = np.mgrid[0:sh, 0:sw]
    m = ((xx - sw * 0.62) / (sw * 0.5)) ** 2 + ((yy - sh / 2) / (sh / 2)) ** 2 < 1
    s = np.zeros((sh, sw, 4), np.uint8)
    s[m] = (58, 10, 9, 235)
    s[m & (yy < 3)] = (84, 16, 14, 235)  # brilho do sangue molhado
    Image.fromarray(s, "RGBA").save(os.path.join(SAIDA, "sangue_chefe.png"))
    print("corpo", corpo.shape[1], "x", corpo.shape[0], "cabeça", cabeca.shape[1])


if __name__ == "__main__":
    main()
