"""
Cursor do mouse no estilo de RPG: uma ponta facetada (a ponta do alto à esquerda é o clique), moldura de osso/latão
com chanfro (claro em cima e à esquerda, escuro embaixo e à direita) e uma pedra escura no meio, com uma faceta de luz.
Uso: python3 cursor_rpg.py <saida.png> [tamanho]     (padrão: 48 px; o clique fica no canto de cima à esquerda)
"""
import sys

from PIL import Image, ImageDraw

OUT = sys.argv[1] if len(sys.argv) > 1 else 'cursor_rpg.png'
SIZE = int(sys.argv[2]) if len(sys.argv) > 2 else 48
SS = 8
W = SIZE * SS


def lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def inset(poly, center, t):
    return [lerp(p, center, t) for p in poly]


# contorno de fora (unidades de 0 a 1): ponta, alto, direita, baixo-direita, baixo-esquerda
outer = [(0.03, 0.02), (0.56, 0.17), (0.93, 0.60), (0.80, 0.95), (0.20, 0.92)]
center = (0.50, 0.55)
mid = inset(outer, center, 0.20)      # meio do chanfro
inner = inset(outer, center, 0.42)    # borda da pedra
gem = inset(outer, center, 0.50)      # a pedra

px = lambda pts: [(x * W, y * W) for x, y in pts]

img = Image.new('RGBA', (W, W), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# contorno escuro (lê bem em cima do cenário escuro e do claro)
d.polygon(px(outer), fill=(18, 14, 10, 255))
o2 = inset(outer, center, 0.045)

# cada lado do chanfro com um tom: luz vindo do alto à esquerda
light = [(236, 222, 190), (214, 196, 158), (160, 138, 100), (128, 108, 76), (196, 176, 136)]
inner_light = [(150, 128, 92), (176, 156, 118), (232, 216, 180), (214, 198, 160), (168, 148, 110)]
n = len(o2)
for i in range(n):
    a, b = o2[i], o2[(i + 1) % n]
    ma, mb = mid[i], mid[(i + 1) % n]
    ia, ib = inner[i], inner[(i + 1) % n]
    d.polygon(px([a, b, mb, ma]), fill=light[i] + (255,))          # chanfro de fora
    d.polygon(px([ma, mb, ib, ia]), fill=inner_light[i] + (255,))  # chanfro de dentro (luz invertida)
    d.line(px([ma, mb]), fill=(90, 74, 50, 255), width=SS // 2)

# a pedra escura, com uma faceta de luz da ponta até o meio
d.polygon(px(inner), fill=(30, 26, 22, 255))
d.polygon(px(gem), fill=(58, 56, 56, 255))
gc = (sum(p[0] for p in gem) / n, sum(p[1] for p in gem) / n)
d.polygon(px([gem[0], gem[1], gc]), fill=(118, 116, 118, 255))
d.polygon(px([gem[0], gc, gem[4]]), fill=(84, 82, 84, 255))
d.polygon(px([gem[2], gem[3], gc]), fill=(36, 34, 36, 255))
d.line(px([gem[0], gc]), fill=(170, 168, 170, 255), width=SS // 2)

img = img.resize((SIZE, SIZE), Image.LANCZOS)
img.save(OUT)
tip = outer[0]
print(f'ok {OUT} {SIZE}px, clique em ({round(tip[0] * SIZE)}, {round(tip[1] * SIZE)})')
