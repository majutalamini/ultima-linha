"""
Cursor do mouse do jogo, em pixel art na paleta da interface (a mesma da tecla E e da placa do menu):
seta clássica com contorno quase preto, aro creme gasto e o miolo vermelho-sangue, com a luz da fluorescente
do vagão no lado esquerdo e a diagonal na sombra. Cada pixel da arte vira 2x2 na tela,
como os personagens em 1080p. A ponta (o clique) fica no canto de cima à esquerda.
Uso: python3 cursor_rpg.py <saida.png>
"""
import sys

import numpy as np
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else 'cursor_rpg.png'
ESCALA, CANVAS = 2, 48

CONTORNO = (16, 13, 11, 255)
ARO = (206, 191, 162, 255)       # creme gasto
ARO_SOMBRA = (150, 134, 106, 255)
MIOLO = (112, 20, 16, 255)       # vermelho do menu, escurecido
MIOLO_LUZ = (168, 36, 26, 255)
MIOLO_ESCURO = (72, 12, 10, 255)

# seta clássica desenhada pixel a pixel: X = contorno, . = dentro (aro creme na beirada, miolo vermelho no meio)
SETA = [
    "X           ",
    "XX          ",
    "X.X         ",
    "X..X        ",
    "X...X       ",
    "X....X      ",
    "X.....X     ",
    "X......X    ",
    "X.......X   ",
    "X........X  ",
    "X.........X ",
    "X......XXXXX",
    "X...X..X    ",
    "X..XX..X    ",
    "X.X  X..X   ",
    "XX   X..X   ",
    "X     X..X  ",
    "      X..X  ",
    "       XX   ",
]
H, W = len(SETA), len(SETA[0])
borda = np.array([[c == 'X' for c in row] for row in SETA])
dentro = np.array([[c == '.' for c in row] for row in SETA])


def encosta(mask, alvo):
    """Pixels de mask vizinhos (4 lados) de algum pixel de alvo."""
    pad = np.pad(alvo, 1)
    viz = pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]
    return mask & viz


aro = encosta(dentro, borda)
miolo = dentro & ~aro

img = np.zeros((H, W, 4), np.uint8)
img[borda] = CONTORNO
img[aro] = ARO
img[miolo] = MIOLO
for y in range(H):
    for x in range(W):
        if aro[y, x] and (y >= 11 or x >= y - 1):           # aro da diagonal e de baixo, na sombra
            img[y, x] = ARO_SOMBRA
        if miolo[y, x]:
            if x <= 2:
                img[y, x] = MIOLO_LUZ                       # luz da fluorescente de cima, no lado esquerdo
            elif x >= y - 3:
                img[y, x] = MIOLO_ESCURO                    # perto da diagonal, mais escuro
big = np.repeat(np.repeat(img, ESCALA, 0), ESCALA, 1)
out = np.zeros((CANVAS, CANVAS, 4), np.uint8)
out[1:1 + big.shape[0], 1:1 + big.shape[1]] = big
Image.fromarray(out).save(OUT)
print(f'ok {OUT}: seta {W}x{H} px de arte, {CANVAS}x{CANVAS} na tela; clique em (2, 2)')
