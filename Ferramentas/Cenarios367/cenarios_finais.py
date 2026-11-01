"""
Cenários 3 (estação), 6 (túnel) e 7 (plataforma final).
Gera em out/Art/Cenario3, Cenario6, Cenario7 e out/layout367.json.
"""
import json, os
import numpy as np
from PIL import Image
from artlib import *

H = 941
FLOOR, CEIL = 850, 112
rng = np.random.default_rng(3)
for n in (3, 6, 7):
    os.makedirs(f'out/Art/Cenario{n}', exist_ok=True)

def tubes(c, xs, w=240, y=100):
    for x in xs:
        c.rect(x - w // 2, y, x + w // 2, y + 12, (150, 156, 148))
        c.rect(x - w // 2, y + 10, x + w // 2, y + 14, (40, 42, 40))

def door_art(folder, prefix, w, h, base, hi, window=True):
    lw = w // 2
    for side in (0, 1):
        k = Canvas(lw, h, base)
        k.grad(0, 0, lw, h, hi, base, 0, tex=5, seed=200 + side)
        k.rect(0 if side else lw - 8, 0, 8 if side else lw, h, (14, 14, 14))
        k.rect(0, 0, lw, 5, (150, 150, 140), alpha=0.35)
        if window:
            k.rect(14, 50, lw - 16, 230, (20, 22, 22))
            k.rect(20, 56, lw - 22, 224, (64, 74, 74), tex=3, seed=220)
            k.rect(20, 56, lw - 22, 224, (8, 10, 12), alpha=0.35)
        k.rect(14, 300, lw - 16, h - 60, hi, tex=3, seed=230 + side, alpha=0.35)
        k.rect(14, 300, lw - 16, 304, (14, 14, 14)); k.rect(14, h - 62, lw - 16, h - 58, (14, 14, 14))
        k.ellipse(lw - 22 if not side else 22, 380, 6, 6, (176, 170, 150))
        k.grime(seed=240 + side, amount=7, streaks=False)
        k.vignette(0.25)
        k.finish(grain=1.6).save(f'out/Art/{folder}/{prefix}_folha_{"dir" if side else "esq"}.png')
    i = Canvas(w, h, (8, 8, 9))
    i.grad(0, 0, w, h, (16, 16, 18), (4, 4, 5), 0)
    i.finish(grain=1.2).save(f'out/Art/{folder}/{prefix}_interior.png')

# =====================================================================  CENÁRIO 3
W3 = 4800
L3 = dict(W=W3, FLOOR=FLOOR, LWALL=120, RWALL=4700, SPAWN=330, SOUTA=450, REN=540,
          TIMETABLE=1010, TICKET=1900, BOOTH=3350, RECEIPT=4100,
          DECK=(3000, 3700, 600), STEPS=[(2780, 2900, 740), (2890, 3010, 670)],
          DOOR=(4450, 4630, 300, FLOOR), KEYPAD=4400,
          TUBES=[(500, 240), (1300, 240), (2100, 240), (2900, 240), (3700, 240), (4400, 240)])
c = Canvas(W3, H, (24, 26, 26))
c.grad(0, 0, W3, 560, (70, 76, 72), (56, 62, 60), 0, tex=3, seed=1)          # parede acima dos azulejos
for x in range(120, 4700, 300):
    c.rect(x, 0, x + 14, 560, (40, 44, 42), alpha=0.7)                         # pilares
c.grad(0, 560, W3, FLOOR, (92, 96, 88), (70, 74, 68), 0, tex=3, seed=2)       # azulejos
for x in range(120, 4700, 60):
    c.rect(x, 560, x + 2, FLOOR, (40, 42, 38), alpha=0.5)
for y in range(560, FLOOR, 48):
    c.rect(0, y, W3, y + 2, (40, 42, 38), alpha=0.5)
c.rect(0, 552, W3, 566, (30, 80, 70))                                          # faixa verde
c.rect(0, 0, W3, CEIL, (28, 30, 30), tex=3, seed=3)
for x in range(0, W3, 150):
    c.rect(x, 0, x + 4, CEIL, (12, 12, 12))
c.rect(0, CEIL - 5, W3, CEIL, (10, 10, 10))
tubes(c, [t[0] for t in L3['TUBES']])
c.text(640, 200, 'ESTAÇÃO', 64, (200, 190, 160), anchor='ma', alpha=0.85, spacing=18)
c.text(640, 280, 'LINHA ÚNICA', 22, (150, 150, 130), anchor='ma', alpha=0.8, spacing=12)
for x, y, r in ((300, 220, 110), (2200, 300, 130), (3900, 200, 120)):
    c.ellipse(x, y, r, r * 1.3, (34, 38, 34), alpha=0.5, blur=28)
# quadro de horários
c.rect(900, 330, 1120, 600, (14, 14, 14))
c.rect(906, 336, 1114, 594, (20, 28, 30))
c.text(1010, 350, 'HORÁRIOS', 18, (180, 170, 130), anchor='ma', spacing=8)
for i in range(9):
    c.rect(920, 390 + i * 22, 1000, 394 + i * 22, (120, 114, 90), alpha=0.5)
    c.rect(1020, 390 + i * 22, 1096, 394 + i * 22, (120, 114, 90), alpha=0.35)
# relógio parado
c.ellipse(1500, 380, 62, 62, (14, 14, 14)); c.ellipse(1500, 380, 54, 54, (190, 186, 170))
c.line([(1500, 380), (1500, 336)], (20, 20, 20), 4); c.line([(1500, 380), (1530, 396)], (20, 20, 20), 3)
# bilheteria
c.rect(1700, 420, 2300, FLOOR, (46, 40, 34), tex=3, seed=9)
c.rect(1690, 410, 2310, 430, (80, 66, 50))
for bx in (1740, 2020):
    c.rect(bx, 470, bx + 240, 640, (10, 14, 16)); c.rect(bx + 6, 476, bx + 234, 634, (56, 68, 70), alpha=0.5)
    c.rect(bx - 6, 640, bx + 246, 654, (110, 90, 66))
c.text(2000, 436, 'BILHETES', 18, (200, 190, 150), anchor='ma', spacing=8)
# bancos
for bx in (1250, 2420, 3100 - 40):
    pass
for bx in (1260, 2400):
    c.rect(bx, 790, bx + 260, 804, (84, 60, 42), tex=3, seed=bx)
    c.rect(bx + 16, 804, bx + 28, FLOOR, (22, 22, 22)); c.rect(bx + 232, 804, bx + 244, FLOOR, (22, 22, 22))
    c.rect(bx, 750, bx + 260, 764, (84, 60, 42), tex=3, seed=bx + 1)
# papel no chão (bilhete)
c.poly([(1880, 840), (1922, 838), (1920, 850), (1876, 851)], (190, 184, 160), alpha=0.9)
c.glow(1900, 846, 60, 14, (120, 110, 70), 0.5)
# degraus (caixotes) e mezanino
for x0, x1, top in L3['STEPS']:
    c.rect(x0, top, x1, FLOOR, (96, 74, 48), tex=4, seed=x0)
    c.rect(x0, top, x1, top + 5, (150, 120, 84), alpha=0.6)
    for k in range(x0 + 20, x1, 36):
        c.rect(k, top + 6, k + 3, FLOOR, (50, 38, 24), alpha=0.7)
d0, d1, dy = L3['DECK']
c.rect(d0, dy - 160, d1, dy, (30, 32, 32), tex=3, seed=12)                  # guichê da equipe: sala
c.rect(d0 + 40, dy - 130, d1 - 40, dy - 20, (18, 22, 22))
c.glow((d0 + d1) // 2, dy - 80, 220, 50, (60, 58, 34), 0.8)
c.rect(3250, dy - 64, 3460, dy - 54, (84, 62, 44))                         # mesa
c.rect(3260, dy - 54, 3270, dy, (50, 38, 28)); c.rect(3440, dy - 54, 3450, dy, (50, 38, 28))
c.rect(3300, dy - 74, 3340, dy - 64, (190, 184, 160), alpha=0.9)
c.text((d0 + d1) // 2, dy - 150, 'FUNCIONÁRIOS', 16, (170, 160, 120), anchor='ma', spacing=8, alpha=0.9)
c.rect(d0 - 10, dy, d1 + 10, dy + 26, (70, 72, 70), tex=3, seed=13)
c.rect(d0 - 10, dy, d1 + 10, dy + 4, (150, 154, 146), alpha=0.55)
for x in range(d0, d1 + 1, 70):                                              # grade do mezanino (corrimão traseiro nao)
    c.rect(x, dy - 4, x + 4, dy, (30, 30, 30))
for px in (d0 + 6, d1 - 24):
    c.rect(px, dy + 26, px + 18, FLOOR, (56, 58, 56), tex=3, seed=px)
c.rect(d0 - 10, dy + 26, d1 + 10, dy + 36, (12, 12, 12), alpha=0.4)
# máquina de venda
c.rect(4020, 560, 4180, FLOOR, (30, 44, 56), tex=3, seed=21)
c.rect(4020, 560, 4180, 566, (100, 130, 150), alpha=0.5)
c.rect(4036, 580, 4140, 760, (10, 16, 20)); c.rect(4040, 584, 4136, 756, (38, 62, 70), alpha=0.5)
for r in range(4):
    for q in range(3):
        c.rect(4048 + q * 30, 598 + r * 40, 4070 + q * 30, 622 + r * 40, (150, 90, 60), alpha=0.7)
c.rect(4146, 590, 4172, 700, (14, 14, 14)); c.rect(4040, 780, 4136, 836, (6, 8, 10))
c.glow(4090, 660, 120, 150, (30, 52, 62), 0.7)
c.poly([(4112, 836), (4134, 836), (4130, 846), (4114, 846)], (190, 184, 160), alpha=0.8)
# porta da administração
dd0, dd1, dt, db = L3['DOOR']
c.rect(dd0 - 20, dt - 20, dd1 + 20, db, (22, 24, 22))
c.text((dd0 + dd1) // 2, dt - 56, 'ADMINISTRAÇÃO', 18, (190, 70, 56), anchor='ma', spacing=6, alpha=0.9)
kx = L3['KEYPAD']
c.rect(kx - 26, 560, kx + 26, 660, (14, 14, 14)); c.rect(kx - 20, 566, kx + 20, 592, (30, 90, 70))
for r in range(3):
    for q in range(3):
        c.rect(kx - 18 + q * 14, 600 + r * 16, kx - 8 + q * 14, 612 + r * 16, (140, 140, 130))
c.grime(seed=31, amount=10)
c.vignette(0.5)
c.finish(grain=2.2).save('out/Art/Cenario3/cenario3_fundo.png')
door_art('Cenario3', 'porta3', dd1 - dd0, db - dt, (58, 64, 62), (86, 94, 90))
json.dump({k: v for k, v in L3.items()}, open('out/layout3.json', 'w'))

# =====================================================================  CENÁRIO 6
W6 = 6200
L6 = dict(W=W6, FLOOR=FLOOR, LWALL=120, RWALL=6140, SPAWN=330, SOCKET=530,
          SLABS=[1900, 3000, 4100, 5100], SLAB=90, DOORTOP=478,
          TRAIN=(5700, 6200), DOOR=(5880, 6040, 400, FLOOR),
          TUBES=[(700, 240), (1450, 240), (2450, 240), (3550, 240), (4600, 240), (5400, 240)])
c = Canvas(W6, H, (20, 21, 22))
c.grad(0, 0, W6, FLOOR, (46, 46, 44), (30, 30, 29), 0, tex=4, seed=41)
for x in range(0, W6, 300):                                                  # costelas do túnel
    c.rect(x, 0, x + 26, FLOOR, (24, 24, 23))
    c.rect(x + 26, 0, x + 30, FLOOR, (74, 72, 66), alpha=0.35)
for x, y, r in ((900, 300, 120), (2700, 380, 140), (4300, 280, 110), (5300, 450, 100)):
    c.ellipse(x, y, r, r * 1.5, (22, 24, 22), alpha=0.5, blur=26)
c.rect(0, 0, W6, CEIL, (22, 22, 22), tex=3, seed=42)
for y in (30, 60, 90):                                                       # cabos
    c.rect(0, y, W6, y + 4, (10, 10, 10))
tubes(c, [t[0] for t in L6['TUBES']])
c.rect(0, 740, W6, 748, (80, 58, 40), alpha=0.8)                             # tubulação ao longo da parede
c.rect(0, 748, W6, 752, (10, 10, 10), alpha=0.5)
c.rect(0, FLOOR, W6, H, (22, 20, 18), tex=4, seed=43)                        # balastro
c.rect(0, FLOOR, W6, FLOOR + 6, (96, 90, 78), alpha=0.7)
for x in range(0, W6, 44):
    c.rect(x, FLOOR + 14, x + 26, FLOOR + 26, (50, 40, 32))
c.rect(0, FLOOR + 30, W6, FLOOR + 38, (110, 106, 98))
# painel do soquete
c.rect(400, 540, 660, 780, (30, 32, 32), tex=3, seed=44)
c.rect(400, 540, 660, 546, (130, 130, 120), alpha=0.5)
c.rect(414, 556, 646, 640, (10, 14, 14)); c.rect(418, 560, 642, 636, (26, 54, 48), alpha=0.5)
c.text(530, 570, 'TRAÇÃO', 22, (150, 190, 170), anchor='ma', spacing=10, alpha=0.9)
c.ellipse(530, 700, 34, 34, (12, 12, 12)); c.ellipse(530, 700, 24, 24, (4, 4, 4))
c.rect(520, 684, 540, 716, (50, 50, 48)); c.rect(500, 696, 560, 704, (150, 36, 28), alpha=0.8)
for lx in (440, 620):
    c.ellipse(lx, 600, 8, 8, (150, 30, 24)); c.glow(lx, 600, 30, 30, (90, 20, 14), 0.8)
# luzes de alarme vermelhas no teto
for ax in (900, 2400, 3550, 4650, 5500):
    c.rect(ax - 18, CEIL, ax + 18, CEIL + 22, (60, 14, 12)); c.glow(ax, CEIL + 12, 90, 70, (110, 20, 14), 0.9)
# anteparas
SL = L6['SLAB']
for sx in L6['SLABS']:
    c.rect(sx, 0, sx + SL, L6['DOORTOP'], (36, 38, 38), tex=3, seed=sx)
    c.rect(sx, 0, sx + 6, L6['DOORTOP'], (96, 100, 96), alpha=0.5)
    c.rect(sx + SL - 6, 0, sx + SL, L6['DOORTOP'], (12, 12, 12))
    for post in (sx, sx + SL - 10):
        c.rect(post, L6['DOORTOP'], post + 10, FLOOR, (46, 48, 48), tex=3, seed=post)
    c.rect(sx, L6['DOORTOP'] - 8, sx + SL, L6['DOORTOP'], (200, 160, 36), alpha=0.9)
    for k in range(sx, sx + SL, 20):
        c.poly([(k, L6['DOORTOP'] - 8), (k + 10, L6['DOORTOP'] - 8), (k + 4, L6['DOORTOP']), (k - 6, L6['DOORTOP'])], (20, 20, 20), alpha=0.9)
# o trem parado, de frente para o fim
t0, t1 = L6['TRAIN']
c.rect(t0, 300, t1, FLOOR + 20, (52, 56, 60), tex=3, seed=61)
c.rect(t0, 300, t1, 308, (140, 146, 150), alpha=0.55)
c.rect(t0, 340, t1, 350, (150, 40, 32))
c.rect(t0 - 14, 300, t0, FLOOR + 20, (20, 22, 24))
for wx in (5740, 5780):
    c.rect(wx, 440, wx + 24, 640, (18, 24, 26)); c.rect(wx + 3, 443, wx + 21, 637, (150, 140, 90), alpha=0.5)
c.glow(5960, 560, 220, 260, (120, 100, 50), 0.6)
c.text(5960, 322, 'ÚLTIMA LINHA', 16, (220, 214, 190), anchor='ma', spacing=10, alpha=0.9)
c.rect(5880 - 12, 400 - 12, 6040 + 12, FLOOR, (24, 26, 28))
c.grime(seed=51, amount=11)
c.vignette(0.55)
c.finish(grain=2.2).save('out/Art/Cenario6/cenario6_fundo.png')
d0, d1, dt, db = L6['DOOR']
door_art('Cenario6', 'porta6', d1 - d0, db - dt, (64, 68, 72), (96, 102, 106), window=True)
json.dump(L6, open('out/layout6.json', 'w'))

# =====================================================================  CENÁRIO 7
W7 = 3600
L7 = dict(W=W7, FLOOR=FLOOR, LWALL=120, RWALL=2660, SPAWN=330, REN=2250, SOUTA=2540, EDGE=2700, RAIL=898,
          GUN=2520, TUBES=[(500, 240), (1300, 240), (2100, 240), (2900, 240)])
c = Canvas(W7, H, (14, 15, 18))
c.grad(0, 0, W7, 600, (44, 50, 54), (34, 38, 42), 0, tex=3, seed=71)
c.grad(0, 600, W7, FLOOR, (60, 64, 62), (42, 46, 46), 0, tex=3, seed=72)
for x in range(120, W7, 60):
    c.rect(x, 600, x + 2, FLOOR, (24, 26, 26), alpha=0.5)
c.rect(0, 592, W7, 606, (60, 28, 24))
for x in range(120, W7, 400):
    c.rect(x, 0, x + 18, FLOOR, (24, 26, 28), alpha=0.8)
c.rect(0, 0, W7, CEIL, (20, 22, 22), tex=3, seed=73)
c.rect(0, CEIL - 5, W7, CEIL, (8, 8, 8))
tubes(c, [t[0] for t in L7['TUBES']])
c.text(1300, 230, 'ÚLTIMA LINHA', 74, (210, 196, 160), anchor='ma', alpha=0.85, spacing=20)
c.text(1300, 326, 'PLATAFORMA ÚNICA', 20, (150, 150, 130), anchor='ma', alpha=0.8, spacing=12)
for bx in (720, 1500):
    c.rect(bx, 790, bx + 260, 804, (74, 56, 42), tex=3, seed=bx)
    c.rect(bx + 16, 804, bx + 28, FLOOR, (20, 20, 20)); c.rect(bx + 232, 804, bx + 244, FLOOR, (20, 20, 20))
    c.rect(bx, 752, bx + 260, 764, (74, 56, 42), tex=3, seed=bx + 1)
c.rect(0, FLOOR, 2700, H, (36, 38, 36), tex=3, seed=74)                      # plataforma
c.rect(0, FLOOR, 2700, FLOOR + 6, (130, 132, 124), alpha=0.5)
c.rect(2600, FLOOR - 2, 2700, FLOOR + 10, (200, 170, 40))                    # faixa amarela
for k in range(2600, 2700, 20):
    c.poly([(k, FLOOR + 10), (k + 10, FLOOR + 10), (k + 20, FLOOR - 2), (k + 10, FLOOR - 2)], (20, 20, 20), alpha=0.9)
c.rect(2700, FLOOR, W7, H, (8, 8, 10))                                       # vala dos trilhos
c.grad(2700, FLOOR, W7, H, (14, 14, 16), (4, 4, 6), 0)
c.rect(2700, FLOOR, 2712, H, (50, 52, 50))
for x in range(2720, W7, 46):
    c.rect(x, 904, x + 28, 914, (44, 36, 28))
c.rect(2700, 892, W7, 900, (110, 106, 98)); c.rect(2700, 892, W7, 894, (170, 168, 160), alpha=0.6)
# túnel à direita (de onde vem o trem)
c.rect(3180, 300, W7, 892, (4, 4, 5))
c.ellipse(3400, 300, 360, 150, (4, 4, 5))
c.rect(3150, 300, 3180, 892, (30, 32, 32))
c.ellipse(3140, 400, 8, 8, (170, 30, 24)); c.glow(3140, 400, 70, 70, (110, 20, 14), 0.9)
c.glow(2300, 830, 400, 40, (40, 48, 56), 0.5)
c.grime(seed=81, amount=10)
c.vignette(0.55)
c.finish(grain=2.2).save('out/Art/Cenario7/cenario7_fundo.png')
json.dump(L7, open('out/layout7.json', 'w'))
print('ok finais')
