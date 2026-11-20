"""Tecla E do aviso de interação, em pixel art na grade dos personagens (58 px/u, filtro Point).
Grava Assets/Art/Cenario1/Objetos/tecla_e_pixel.png. Uso: python Ferramentas/Interface/tecla_e.py"""
import os
import numpy as np
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Cenario1", "Objetos", "tecla_e_pixel.png")

W, H, FUNDO = 30, 32, 5        # tecla 30x27 + 5 px de lateral embaixo (a tecla "funda")
BORDA = (20, 17, 15, 255)
LATERAL = (52, 46, 40, 255)
FACE = (38, 34, 30, 255)
LUZ = (78, 70, 60, 255)          # brilho no alto da face (fluorescente de cima)
CONTORNO = (176, 160, 134, 255)  # creme gasto da paleta
LETRA = (232, 220, 200, 255)
E = ["11111", "1....", "1....", "1111.", "1....", "1....", "11111"]  # 5x7, cada pixel vira 2x2


def tecla():
    a = np.zeros((H, W, 4), np.uint8)
    face_h = H - FUNDO
    a[1:H - 1, 1:W - 1] = BORDA             # contorno escuro de fora
    a[face_h - 2:H - 2, 2:W - 2] = LATERAL  # lateral (a parte de baixo da tecla)
    a[2:face_h - 2, 2:W - 2] = CONTORNO     # aro claro
    a[3:face_h - 3, 3:W - 3] = FACE
    a[3:5, 4:W - 4] = LUZ
    for c in ((1, 1), (1, W - 2), (H - 2, 1), (H - 2, W - 2)):  # cantos arredondados
        a[c] = 0
    x0, y0 = (W - 10) // 2, 3 + (face_h - 6 - 14) // 2
    for j, linha in enumerate(E):
        for i, ch in enumerate(linha):
            if ch == "1":
                a[y0 + 2 * j:y0 + 2 * j + 2, x0 + 2 * i:x0 + 2 * i + 2] = LETRA
    return a


if __name__ == "__main__":
    t = tecla()
    assert t.shape == (H, W, 4) and t[..., 3].any()
    Image.fromarray(t, "RGBA").save(SAIDA)
    print(SAIDA)
