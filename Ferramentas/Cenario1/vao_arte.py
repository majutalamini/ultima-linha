"""
Abismo e beiradas do vão entre os vagões (Cenários 0, 1 e 2): sem ele, os trilhos e o cascalho do túnel apareciam bem na
altura do piso e o vão parecia ter chão. Do piso para baixo é preto (com um pontilhado de pixel art na borda);
logo acima do piso, uma sombra fraca escurece o pé da parede do túnel, para a beirada das plataformas ficar nítida.
Uma faixa estreita (8 px) que o construtor estica na largura do vão; o pivô fica na linha do piso.
Uso: python3 vao_arte.py <pasta Objetos>   (ex.: Assets/Art/Cenario1/Objetos)
"""
import os
import sys

from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out/Objetos'
W, ACIMA, ABAIXO = 8, 16, 100           # px na grade dos personagens (58 por unidade)
H = ACIMA + ABAIXO
os.makedirs(OUT, exist_ok=True)

img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
px = img.load()
bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
for y in range(H):
    for x in range(W):
        if y < ACIMA:                                   # sombra no pé da parede, em degraus pontilhados
            k = (y + 1) / ACIMA
            a = 0.38 * k * k
            if bayer[y % 4][x % 4] / 16 < (a * 4) % 1:
                a += 0.08
            px[x, y] = (6, 4, 4, int(255 * min(a, 0.45)))
        else:                                           # do piso para baixo: o vazio
            d = y - ACIMA
            a = 0.9 if d < 2 else 0.96 if d < 6 else 1.0
            c = 10 if d < 2 else 6 if d < 10 else 3     # um fio mais claro na linha do piso: a beirada
            px[x, y] = (c, c - 2 if c > 2 else c, c - 2 if c > 2 else c, int(255 * a))
img.save(os.path.join(OUT, 'vao_abismo.png'))
print('ok vao_abismo', W, 'x', H, 'pivô no piso:', ABAIXO / H)

# beirada: a ponta da plataforma, com faixa de perigo amarela e preta no piso e a face de aço caindo no escuro.
# Desenhada para a beirada da esquerda do vão (a plataforma fica à esquerda); a da direita é a mesma espelhada.
BW, BH, FAIXA = 14, 36, 5
b = Image.new('RGBA', (BW, BH), (0, 0, 0, 0))
q = b.load()
amarelo, preto = (201, 160, 39), (24, 20, 14)
for y in range(BH):
    for x in range(BW):
        if y < FAIXA:                                   # faixa de perigo diagonal no piso
            listra = ((x + y) // 3) % 2 == 0
            c = amarelo if listra else preto
            if y == 0:
                c = tuple(min(255, v + 40) for v in c)  # a quina pega luz
            q[x, y] = c + (255,)
        else:                                           # face de aço da ponta, escurecendo para baixo
            k = (y - FAIXA) / (BH - FAIXA)
            base = 72 - 60 * k
            if x == BW - 1:
                base += 26 * (1 - k)                    # quina vertical da beirada, iluminada
            elif x == BW - 2:
                base *= 0.7
            if (y - FAIXA) % 9 == 3 and x in (3, BW - 5):
                base += 18                              # rebites
            v = int(max(4, base))
            q[x, y] = (v, int(v * 0.95), int(v * 0.9), 255)
b.save(os.path.join(OUT, 'vao_beirada.png'))
print('ok vao_beirada', BW, 'x', BH)
