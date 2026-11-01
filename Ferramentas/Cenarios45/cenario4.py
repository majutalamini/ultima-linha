"""
Cenário 4 — a sala de controle (reativar o trem).
Gera em out/Art/Cenario4/: cenario4_fundo.png, porta4_folha_esq.png, porta4_folha_dir.png, porta4_interior.png,
gaveta_aberta.png  e  out/Cenarios45Layout4.json (medidas para o builder da Unity).
Tudo pintado por código: nada de IA, nada animado (sem "chiado").
"""
import json, os, math
import numpy as np
from PIL import Image
from artlib import *

OUT = 'out/Art/Cenario4'
os.makedirs(OUT, exist_ok=True)
W, H = 3400, 941
FLOOR, CEIL = 850, 112
LWALL, RWALL = 120, 3340

# medidas (px, y para baixo) — o builder usa os mesmos números
L = dict(
    W=W, H=H, FLOOR=FLOOR, LWALL=LWALL, RWALL=RWALL,
    CRATE=(1740, 1900, 720), CATWALK=(1900, 2600, 590), GEN=(2650, 2900, 640),
    LEVERS=[(1650, FLOOR), (2050, 590), (2780, 640)],
    SOCKET=(3000, FLOOR), DESK=(760, 1080, 700), DRAWER=(900, FLOOR), NOTE=(660, FLOOR), CLOCK=(1250, FLOOR),
    DOOR=(3120, 3300, 300, FLOOR), TUBES=[(500, 230), (1300, 230), (2100, 230), (2900, 230)],
    RED=[(380, 150), (1500, 150), (2300, 150), (3200, 150)],
    SPAWN=300, SOUTA=480, REN=570,
)
json.dump(L, open('out/layout4.json', 'w'))

c = Canvas(W, H, (22, 24, 23))

# ---------- parede ----------
c.grad(0, 0, W, 600, (46, 49, 48), (38, 41, 40), 0, tex=3, seed=4)           # concreto
c.grad(0, 600, W, FLOOR, (32, 40, 37), (24, 30, 28), 0, tex=3, seed=5)         # rodapé de tinta verde
c.rect(0, 596, W, 604, (60, 66, 62), tex=2)                                    # friso
c.rect(0, 604, W, 608, (14, 16, 15))
# juntas das placas de concreto
for x in range(LWALL, RWALL, 340):
    c.rect(x, 0, x + 3, 596, (24, 26, 25))
    c.rect(x + 3, 0, x + 5, 596, (62, 66, 64), alpha=0.5)
# rachadura
c.line([(1040, 160), (1058, 230), (1046, 300), (1070, 380), (1062, 430)], (18, 19, 19), 3)
c.line([(1058, 230), (1090, 262), (1100, 310)], (18, 19, 19), 2)

# ---------- teto, canos e cabos ----------
c.grad(0, 0, W, CEIL, (14, 15, 15), (30, 32, 31), 0, tex=3, seed=6)
c.rect(0, CEIL - 6, W, CEIL, (10, 10, 10))
for y, r, col in ((46, 11, (58, 62, 60)), (78, 8, (50, 54, 52))):
    c.rect(0, y - r, W, y + r, col, tex=3, seed=y)
    c.rect(0, y - r, W, y - r + 3, (95, 100, 96), alpha=0.5)
    c.rect(0, y + r - 4, W, y + r, (12, 12, 12), alpha=0.7)
    for x in range(150, W, 420):
        c.rect(x, y - r - 3, x + 12, y + r + 3, (36, 38, 37), tex=2)
# cabos pendurados
for x0, x1, sag in ((400, 760, 40), (1500, 2000, 55), (2300, 2800, 45)):
    pts = [(x0 + (x1 - x0) * t / 20, 100 + sag * math.sin(math.pi * t / 20)) for t in range(21)]
    c.line(pts, (10, 10, 10), 4)
    c.line([(x, y - 1) for x, y in pts], (46, 48, 47), 1)
# canos verticais
for x in (300, 1450, 2850):
    c.rect(x - 10, CEIL, x + 10, 700, (48, 52, 50), tex=3, seed=x)
    c.rect(x - 10, CEIL, x - 6, 700, (96, 102, 98), alpha=0.45)
    c.rect(x + 6, CEIL, x + 10, 700, (12, 12, 12), alpha=0.6)
    for y in range(180, 700, 170):
        c.rect(x - 14, y, x + 14, y + 12, (34, 36, 35))

# ---------- armários (esquerda) ----------
for i, x in enumerate(range(330, 640, 78)):
    c.rect(x, 470, x + 74, FLOOR, (52, 58, 56), tex=3, seed=30 + i)
    c.rect(x, 470, x + 4, FLOOR, (96, 104, 100), alpha=0.5)
    c.rect(x + 70, 470, x + 74, FLOOR, (12, 12, 12), alpha=0.6)
    for k in range(5):
        c.rect(x + 14, 492 + k * 12, x + 60, 497 + k * 12, (18, 20, 19))    # venezianas
    c.rect(x + 8, 610, x + 66, 614, (14, 14, 14))
    c.rect(x + 56, 640, x + 62, 668, (150, 140, 110), alpha=0.8)             # trinco
c.poly([(487, 470), (487, FLOOR), (474, FLOOR - 12), (474, 478)], (24, 26, 25))   # um armário entreaberto
c.rect(487, 470, 492, FLOOR, (12, 12, 12))

# ---------- bilhete preso na parede ----------
nx, ny = 660, 500
c.rect(nx - 46, ny - 62, nx + 46, ny + 62, (156, 148, 124), tex=3, seed=51)
c.rect(nx - 46, ny - 62, nx + 46, ny - 56, (120, 112, 92))
for k in range(9):
    w = 70 - (k % 3) * 14
    c.rect(nx - 36, ny - 38 + k * 12, nx - 36 + w, ny - 35 + k * 12, (62, 56, 46), alpha=0.8)
c.ellipse(nx, ny - 66, 5, 5, (160, 40, 36))
c.ellipse(nx, ny - 66, 2, 2, (220, 120, 110))
c.rect(nx - 46, ny + 56, nx + 46, ny + 64, (0, 0, 0), alpha=0.25)

# ---------- relógio parado em 4:17 ----------
cx, cy, R = 1250, 330, 76
c.ellipse(cx + 3, cy + 5, R + 12, R + 12, (0, 0, 0), alpha=0.45, blur=5)
c.ellipse(cx, cy, R + 10, R + 10, (20, 20, 20))
c.ellipse(cx, cy, R + 6, R + 6, (66, 66, 64))
c.ellipse(cx, cy, R, R, (176, 170, 150), tex=3)
c.ellipse(cx, cy, R, R, (90, 84, 70), alpha=0.35, blur=14)        # sujeira nas bordas
for k in range(60):
    a = math.radians(k * 6)
    big = k % 5 == 0
    r0 = R - (16 if big else 8)
    c.line([(cx + math.sin(a) * r0, cy - math.cos(a) * r0), (cx + math.sin(a) * (R - 3), cy - math.cos(a) * (R - 3))], (36, 32, 28), 3 if big else 1)
for n_, k in ((12, 0), (3, 3), (6, 6), (9, 9)):
    a = math.radians(k * 30)
    c.text(cx + math.sin(a) * (R - 30), cy - math.cos(a) * (R - 30), str(n_), 20, (44, 38, 32), anchor='mm')
hour = math.radians(4 * 30 + 17 * 0.5)
minute = math.radians(17 * 6)
c.line([(cx, cy), (cx + math.sin(hour) * 38, cy - math.cos(hour) * 38)], (28, 24, 22), 6)
c.line([(cx, cy), (cx + math.sin(minute) * 58, cy - math.cos(minute) * 58)], (28, 24, 22), 4)
c.ellipse(cx, cy, 6, 6, (120, 30, 26))
c.line([(cx - 40, cy - 62), (cx - 10, cy - 20), (cx + 8, cy - 8)], (36, 36, 34), 1, alpha=0.8)   # vidro trincado
c.line([(cx - 10, cy - 20), (cx - 34, cy + 10)], (36, 36, 34), 1, alpha=0.7)
c.glow(cx - 28, cy - 30, 40, 26, (60, 64, 66), 0.5)

# ---------- painel de controle (parede do fundo) ----------
bx0, bx1, by0, by1 = 1470, 2800, 168, 560
c.rect(bx0 - 8, by0 - 8, bx1 + 8, by1 + 8, (14, 15, 15))
c.grad(bx0, by0, bx1, by1, (44, 48, 46), (30, 33, 32), 0, tex=3, seed=61)
c.rect(bx0, by0, bx1, by0 + 3, (110, 116, 112), alpha=0.5)
for x in range(bx0 + 20, bx1, 140):
    c.rect(x, by0, x + 2, by1, (16, 17, 16))
    for y in (by0 + 14, by1 - 18):
        c.ellipse(x + 8, y, 3, 3, (86, 90, 86))
        c.ellipse(x + 120, y, 3, 3, (86, 90, 86))
# faixa de lâmpadas (4 encaixes): 3 acenderão com as alavancas, a 4ª fica apagada
c.rect(1960, 200, 2310, 262, (14, 14, 14))
c.text(2135, 207, 'ENERGIA', 18, (150, 142, 120), anchor='ma')
for i in range(4):
    x = 1996 + i * 90
    c.ellipse(x, 242, 10, 10, (40, 14, 12) if i < 3 else (20, 20, 20))
    c.ellipse(x, 242, 10, 10, (190, 40, 30), alpha=0.35 if i < 3 else 0.0)
# medidores
def gauge(x, y, r, needle, seed):
    c.ellipse(x, y, r + 6, r + 6, (16, 16, 16))
    c.ellipse(x, y, r, r, (150, 146, 128), tex=3)
    c.ellipse(x, y, r, r, (70, 64, 52), alpha=0.35, blur=8)
    for k in range(-120, 121, 20):
        a = math.radians(k)
        c.line([(x + math.sin(a) * (r - 10), y - math.cos(a) * (r - 10)), (x + math.sin(a) * (r - 3), y - math.cos(a) * (r - 3))], (36, 32, 28), 2)
    a = math.radians(needle)
    c.line([(x, y), (x + math.sin(a) * (r - 8), y - math.cos(a) * (r - 8))], (150, 34, 28), 3)
    c.ellipse(x, y, 5, 5, (30, 28, 26))
    c.glow(x - r * 0.3, y - r * 0.4, r * 0.7, r * 0.5, (50, 54, 56), 0.5)
for i, (x, y, r, nd) in enumerate([(1560, 260, 52, -95), (1560, 400, 40, -60), (1750, 300, 60, -110), (2430, 280, 56, -100),
                                    (2610, 280, 44, -75), (2700, 420, 38, -90), (2520, 420, 34, -110)]):
    gauge(x, y, r, nd, i)
# placas com letras
for x, y, t in ((1560, 330, 'LINHA'), (1750, 372, 'TENSÃO'), (2430, 346, 'FREIO'), (2610, 330, 'PORTAS')):
    c.rect(x - 38, y, x + 38, y + 18, (18, 18, 18))
    c.text(x, y + 1, t, 14, (140, 132, 108), anchor='ma')
# fileiras de chaves e luzes apagadas
for i in range(10):
    x = 1860 + i * 36
    c.rect(x, 330, x + 20, 352, (20, 20, 20))
    c.rect(x + 8, 336 if i % 3 else 342, x + 12, 350, (96, 100, 96))
    c.ellipse(x + 10, 372, 5, 5, (34, 18, 16) if i % 4 else (110, 24, 20))
# fiação saindo do painel para o teto
for x in (1500, 1530, 2780):
    c.line([(x, by0 - 8), (x + 6, 150), (x, CEIL)], (12, 12, 12), 5)
    c.line([(x - 1, by0 - 8), (x + 5, 150), (x - 1, CEIL)], (50, 54, 52), 1)
# trilho onde as alavancas se apoiam (parte da parede atrás)
for (lx, ly) in L['LEVERS']:
    c.rect(lx - 40, ly - 190, lx + 40, ly - 150, (26, 28, 27))     # caixa de fiação em cima de cada alavanca

# ---------- encaixe vazio (4ª alavanca) ----------
sx, sy = L['SOCKET'][0], 650
c.rect(sx - 38, sy - 94, sx + 38, sy + 80, (36, 40, 38), tex=3, seed=71)
c.rect(sx - 38, sy - 94, sx - 34, sy + 80, (100, 106, 102), alpha=0.5)
c.rect(sx - 24, sy - 66, sx + 24, sy + 36, (6, 6, 6))
c.rect(sx - 18, sy - 40, sx + 18, sy - 34, (150, 98, 54))           # contatos de cobre expostos
c.rect(sx - 18, sy - 20, sx + 18, sy - 14, (150, 98, 54))
c.text(sx, sy + 46, '04', 20, (150, 142, 120), anchor='ma')
c.ellipse(sx, sy - 80, 6, 6, (170, 34, 28))
c.line([(sx + 30, sy + 70), (sx + 52, sy + 60), (sx + 60, FLOOR - 4)], (10, 10, 10), 4)

# ---------- mesa do supervisor ----------
dx0, dx1, dtop = L['DESK']
c.rect(dx0, dtop, dx1, FLOOR, (46, 36, 28), tex=4, seed=81)                   # corpo
c.rect(dx0 - 10, dtop - 4, dx1 + 10, dtop + 14, (84, 66, 48), tex=4, seed=82)  # tampo
c.rect(dx0 - 10, dtop - 4, dx1 + 10, dtop, (130, 108, 80), alpha=0.5)
c.rect(dx0, dtop + 14, dx1, dtop + 20, (10, 10, 10), alpha=0.5)
c.rect(dx0 + 14, dtop + 40, dx0 + 190, dtop + 110, (58, 46, 36), tex=3, seed=83) # frente da gaveta
c.rect(dx0 + 14, dtop + 40, dx0 + 190, dtop + 44, (110, 92, 70), alpha=0.5)
c.rect(dx0 + 14, dtop + 106, dx0 + 190, dtop + 110, (14, 12, 10))
c.rect(dx0 + 90, dtop + 56, dx0 + 118, dtop + 84, (150, 130, 80), tex=2, seed=84)    # placa do cadeado
c.ellipse(dx0 + 104, dtop + 70, 9, 9, (24, 20, 14))
for k in range(4):
    c.ellipse(dx0 + 92 + k * 8, dtop + 92, 2, 2, (20, 18, 12))
c.rect(dx0 + 14, dtop + 120, dx0 + 190, dtop + 150, (40, 32, 24))
c.rect(dx0 + 220, dtop + 30, dx1 - 14, FLOOR - 20, (40, 32, 24))               # porta lateral
c.rect(dx0 + 232, dtop + 44, dx1 - 26, FLOOR - 34, (50, 40, 30), tex=3, seed=85)
c.rect(dx0 + 230, dtop + 40, dx0 + 234, FLOOR - 30, (10, 10, 10), alpha=0.6)
# objetos em cima da mesa
c.rect(dx0 + 30, dtop - 22, dx0 + 90, dtop - 4, (150, 144, 124), tex=3, seed=86)  # pilha de papéis
c.rect(dx0 + 34, dtop - 28, dx0 + 94, dtop - 20, (136, 130, 112), tex=3, seed=87)
c.rect(dx0 + 200, dtop - 38, dx0 + 244, dtop - 4, (30, 32, 30))                  # telefone velho
c.line([(dx0 + 204, dtop - 40), (dx0 + 240, dtop - 40)], (14, 14, 14), 5)
c.rect(dx1 - 90, dtop - 56, dx1 - 74, dtop - 4, (32, 34, 32))                    # luminária de mesa apagada
c.line([(dx1 - 82, dtop - 54), (dx1 - 60, dtop - 76), (dx1 - 36, dtop - 70)], (34, 36, 34), 4)
c.poly([(dx1 - 44, dtop - 72), (dx1 - 18, dtop - 62), (dx1 - 34, dtop - 50)], (46, 48, 46))
c.ellipse(dx1 + 10, dtop - 4, 3, 2, (0, 0, 0), alpha=0.0)
# cadeira caída
c.poly([(1120, FLOOR - 2), (1210, FLOOR - 40), (1224, FLOOR - 24), (1136, FLOOR + 2)], (30, 28, 28))
c.rect(1200, FLOOR - 44, 1228, FLOOR - 4, (36, 34, 34))
c.line([(1130, FLOOR - 6), (1112, FLOOR + 8)], (16, 16, 16), 4)
c.line([(1210, FLOOR - 8), (1226, FLOOR + 8)], (16, 16, 16), 4)

# ---------- caixote, passarela, gerador ----------
cx0, cx1, ctop = L['CRATE']
c.rect(cx0, ctop, cx1, FLOOR, (70, 56, 40), tex=5, seed=91)
for y in range(ctop + 36, FLOOR, 36):
    c.rect(cx0, y, cx1, y + 3, (22, 18, 12))
c.rect(cx0, ctop, cx1, ctop + 6, (128, 104, 74), alpha=0.6)
c.rect(cx0, ctop, cx0 + 14, FLOOR, (36, 40, 38))
c.rect(cx1 - 14, ctop, cx1, FLOOR, (36, 40, 38))
c.rect(cx0, ctop + 52, cx1, ctop + 62, (36, 40, 38))
c.rect(cx0, ctop, cx0 + 4, FLOOR, (110, 116, 112), alpha=0.4)
c.text((cx0 + cx1) // 2, ctop + 82, 'MANUT.', 22, (170, 150, 90), anchor='ma', alpha=0.75)

k0, k1, kt = L['CATWALK']
for x in (k0 + 10, (k0 + k1) // 2, k1 - 14):
    c.rect(x, kt, x + 12, FLOOR, (34, 37, 36), tex=2, seed=x)
    c.rect(x, kt, x + 3, FLOOR, (92, 98, 94), alpha=0.35)
for x in range(k0 + 10, k1 - 10, 120):                                  # escoras em diagonal
    c.line([(x + 6, kt + 20), (x + 112, kt + 160)], (28, 30, 29), 6) if x + 112 < k1 else None
c.rect(k0, kt, k1, kt + 20, (50, 54, 52), tex=3, seed=92)
c.rect(k0, kt, k1, kt + 4, (130, 136, 130), alpha=0.6)
for x in range(k0, k1, 16):                                             # grade
    c.rect(x, kt + 8, x + 3, kt + 18, (12, 12, 12))
c.rect(k0, kt + 20, k1, kt + 26, (0, 0, 0), alpha=0.35)
# corrimão do fundo da passarela
c.rect(k0 + 80, kt - 118, k1 - 20, kt - 110, (60, 64, 62))
for x in range(k0 + 90, k1 - 20, 140):
    c.rect(x, kt - 118, x + 8, kt, (60, 64, 62))

g0, g1, gt = L['GEN']
c.rect(g0, gt, g1, FLOOR, (38, 46, 42), tex=4, seed=93)
c.rect(g0, gt, g1, gt + 6, (110, 118, 112), alpha=0.5)
c.rect(g0, gt, g0 + 5, FLOOR, (100, 108, 102), alpha=0.35)
c.rect(g1 - 8, gt, g1, FLOOR, (12, 14, 13), alpha=0.7)
for i in range(6):                                                       # grade de ventilação
    c.rect(g0 + 26, gt + 40 + i * 14, g0 + 160, gt + 48 + i * 14, (10, 10, 10))
c.rect(g0 + 176, gt + 30, g1 - 20, gt + 120, (22, 24, 23))
gauge(g0 + 212, gt + 76, 28, -80, 99)
c.text(g0 + 125, gt + 160, 'GERADOR 02', 20, (150, 142, 110), anchor='ma', alpha=0.85)
for x in (g0 + 30, g1 - 40):
    c.rect(x, gt - 28, x + 14, gt, (52, 56, 54))
c.ellipse(g0 + 195, gt + 168, 7, 7, (120, 26, 22))
c.glow(g0 + 195, gt + 168, 28, 20, (140, 20, 14), 0.8)
# cabos do gerador para o painel (no chão)
c.line([(g0 - 4, FLOOR - 8), (g0 - 80, FLOOR - 12), (2540, FLOOR - 6), (2380, FLOOR - 10), (2200, FLOOR - 4)], (10, 10, 10), 6)
c.line([(g0 - 4, FLOOR - 10), (g0 - 80, FLOOR - 14), (2540, FLOOR - 8), (2380, FLOOR - 12), (2200, FLOOR - 6)], (44, 46, 45), 1)

# ---------- placas de apoio das alavancas ----------
for (lx, ly) in L['LEVERS']:
    c.rect(lx - 34, ly - 14, lx + 34, ly, (30, 33, 32), tex=2, seed=lx)
    c.rect(lx - 34, ly - 14, lx + 34, ly - 11, (100, 106, 102), alpha=0.5)
    c.ellipse(lx - 26, ly - 7, 3, 3, (16, 16, 16))
    c.ellipse(lx + 26, ly - 7, 3, 3, (16, 16, 16))

# ---------- chão ----------
c.grad(0, FLOOR, W, H, (30, 31, 30), (14, 14, 14), 0, tex=4, seed=101)
c.rect(0, FLOOR, W, FLOOR + 4, (88, 92, 88), alpha=0.5)
c.rect(0, FLOOR, W, FLOOR + 14, (12, 12, 12), alpha=0.35)
for x in range(0, W, 280):
    c.rect(x, FLOOR + 4, x + 3, H, (10, 10, 10), alpha=0.8)
# faixa de segurança amarela e preta junto à porta
for x in range(L['DOOR'][0] - 60, L['DOOR'][1] + 60, 22):
    c.poly([(x, FLOOR + 22), (x + 12, FLOOR + 22), (x + 4, FLOOR + 34), (x - 8, FLOOR + 34)], (150, 120, 30), alpha=0.55)
# poças e manchas
for x, w_ in ((700, 190), (1600, 240), (2400, 160), (3000, 220)):
    c.ellipse(x, FLOOR + 36, w_, 12, (8, 9, 9), alpha=0.7, blur=3)
    c.ellipse(x - w_ * 0.2, FLOOR + 34, w_ * 0.5, 4, (96, 100, 100), alpha=0.25, blur=2)
# papéis no chão
for x, w_, r_ in ((900, 60, 8), (1980, 48, -6), (3050, 52, 5)):
    c.poly([(x, FLOOR + 8), (x + w_, FLOOR + 8 - r_), (x + w_ - 6, FLOOR + 22), (x - 6, FLOOR + 24)], (126, 120, 104), alpha=0.7)

# ---------- porta de serviço (moldura; as folhas são sprites) ----------
d0, d1, dt, db = L['DOOR']
c.rect(d0 - 26, dt - 40, d1 + 26, db, (28, 31, 30), tex=3, seed=111)
c.rect(d0 - 26, dt - 40, d0 - 22, db, (100, 106, 102), alpha=0.4)
c.rect(d0 - 26, dt - 40, d1 + 26, dt - 34, (96, 102, 98), alpha=0.35)
c.rect(d0 - 8, dt - 8, d1 + 8, db, (6, 6, 6))
c.rect(d0 - 26, db - 6, d1 + 26, db, (110, 90, 28), alpha=0.7)
# placa em cima da porta
c.rect(d0 + 20, dt - 80, d1 - 20, dt - 52, (18, 18, 18))
c.text((d0 + d1) // 2, dt - 78, 'SAÍDA TÉCNICA', 18, (156, 148, 120), anchor='ma')

# ---------- entrada (esquerda): de onde eles vieram ----------
c.rect(150, 330, 322, FLOOR, (6, 6, 7))
c.rect(138, 318, 334, 336, (40, 43, 42))
c.rect(138, 318, 150, FLOOR, (40, 43, 42))
c.rect(322, 318, 334, FLOOR, (40, 43, 42))
for i in range(6):                                                       # escada que sobe para a plataforma
    c.rect(150 + i * 4, 740 - i * 54 + 54, 322, 740 - i * 54 + 64, (30 + i * 6, 32 + i * 6, 32 + i * 6))
c.glow(236, 420, 140, 140, (40, 46, 56), 0.7)                             # sobra de luz da estação

# ---------- luzes ----------
for tx, tw in L['TUBES']:
    c.rect(tx - tw // 2, 100, tx + tw // 2, 118, (22, 23, 23))
    c.rect(tx - tw // 2 + 6, 104, tx + tw // 2 - 6, 114, (190, 200, 196))       # tubo (o jogo apaga por cima)
    c.cone(tx, 118, 700, tw * 0.9, tw * 2.8, (46, 52, 50), 1.0)
for rx, ry in L['RED']:
    c.rect(rx - 18, ry - 10, rx + 18, ry + 10, (36, 8, 8))
    c.rect(rx - 12, ry - 6, rx + 12, ry + 6, (140, 22, 18))
    c.glow(rx, ry + 6, 220, 140, (60, 6, 4), 1.0)

c.grime(seed=13, amount=10)
c.vignette(0.55)
im = c.finish(grain=2.2)
im.save(f'{OUT}/cenario4_fundo.png')

# ---------- folhas da porta ----------
lw, lh = (d1 - d0) // 2, db - dt
def leaf(side):
    k = Canvas(lw, lh, (52, 56, 54))
    k.grad(0, 0, lw, lh, (66, 70, 68), (46, 50, 48), 0, tex=4, seed=121 + side)
    k.rect(0, 0, 4, lh, (110, 116, 112), alpha=0.5) if side == 0 else k.rect(lw - 4, 0, lw, lh, (110, 116, 112), alpha=0.5)
    k.rect(0, 0, lw, 5, (120, 126, 120), alpha=0.5)
    k.rect(0 if side else lw - 8, 0, 8 if side else lw, lh, (20, 22, 21))                # fresta do meio
    k.rect(10, 70, lw - 10, 74, (18, 19, 18))
    k.rect(10, lh - 180, lw - 10, lh - 176, (18, 19, 18))
    for y in (28, lh - 40):
        for x in (14, lw - 22):
            k.ellipse(x, y, 4, 4, (24, 26, 25))
    for i in range(0, lw - 20, 22):                                                         # faixa de perigo
        k.poly([(10 + i, lh - 72), (24 + i, lh - 72), (14 + i, lh - 54), (0 + i, lh - 54)], (150, 120, 30), alpha=0.6) if 10 + i + 14 < lw - 10 else None
    k.rect(lw // 2 - 22, 130, lw // 2 + 22, 280, (28, 30, 29))                              # janelinha de vidro aramado
    k.rect(lw // 2 - 18, 134, lw // 2 + 18, 276, (10, 12, 12))
    k.grime(seed=140 + side, amount=8, streaks=False)
    k.vignette(0.25)
    return k.finish(grain=1.6)
leaf(0).save(f'{OUT}/porta4_folha_esq.png')
leaf(1).save(f'{OUT}/porta4_folha_dir.png')
inter = Canvas(d1 - d0, db - dt, (8, 9, 10))
inter.grad(0, 0, d1 - d0, db - dt, (14, 16, 18), (4, 4, 5), 0)
inter.glow((d1 - d0) // 2, 200, 120, 220, (10, 14, 20), 1.0)
inter.finish(grain=1.2).save(f'{OUT}/porta4_interior.png')

# ---------- gaveta aberta (sprite com o brilho da chave) ----------
gw, gh = 220, 96
g = np.zeros((gh, gw, 3), np.float32)
al = np.zeros((gh, gw), np.float32)
gc = Canvas(gw, gh, (0, 0, 0))
gc.rect(0, 0, gw, gh, (0, 0, 0))
gc.rect(6, 26, gw - 6, gh - 8, (52, 42, 32), tex=3, seed=151)         # caixa da gaveta (vista de cima, inclinada)
gc.rect(14, 34, gw - 14, gh - 16, (14, 11, 9))                          # interior
gc.rect(26, 44, 120, 66, (150, 142, 122), tex=3, seed=152)              # papéis
gc.line([(40, 52), (100, 52)], (60, 54, 44), 1)
gc.line([(40, 58), (92, 58)], (60, 54, 44), 1)
gc.ellipse(158, 56, 11, 11, (200, 170, 70))                              # a chave
gc.ellipse(158, 56, 5, 5, (14, 11, 9))
gc.rect(166, 53, 204, 59, (200, 170, 70))
gc.rect(192, 59, 196, 69, (200, 170, 70))
gc.rect(198, 59, 202, 67, (200, 170, 70))
gc.glow(158, 56, 40, 30, (120, 96, 30), 0.9)
gc.rect(0, 0, gw, 30, (62, 50, 38), tex=3, seed=153)                   # frente da gaveta puxada
gc.rect(0, 0, gw, 4, (126, 106, 80), alpha=0.5)
gc.rect(gw // 2 - 24, 10, gw // 2 + 24, 18, (150, 130, 80))
mask = np.zeros((gh, gw), np.float32)
mask[0:gh - 6, 0:gw] = 1.0
save_rgba(gc.a, mask, f'{OUT}/gaveta_aberta.png')
print('ok cenario4')
