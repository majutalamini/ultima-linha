"""
Cenário 5 — o escritório abandonado, onde está o corpo do chefe.
Gera em out/Art/Cenario5/: cenario5_fundo.png, porta5_folha_esq.png, porta5_folha_dir.png, porta5_interior.png,
arquivo_gaveta.png, pistola.png, corpo_chefe.png, cabeca_chefe.png  e  out/layout5.json.
"""
import json, os, math
import numpy as np
from PIL import Image
from artlib import *

OUT = 'out/Art/Cenario5'
os.makedirs(OUT, exist_ok=True)
W, H = 3600, 941
FLOOR, CEIL = 850, 112
LWALL, RWALL = 120, 3540

L = dict(
    W=W, H=H, FLOOR=FLOOR, LWALL=LWALL, RWALL=RWALL,
    BOSSDESK=(1580, 2000, 700), BODY=(2160, 2540), BODYSPOT=2330,
    CABINETS=[('arquivoA', 'ADMISSÕES', 1280, 1410), ('arquivoB', 'AVALIAÇÕES', 2640, 2770),
              ('arquivoC', 'DEMISSÕES', 2810, 2940), ('arquivoD', 'FINANCEIRO', 2980, 3110)],
    DOOR=(3250, 3430, 300, FLOOR), WINDOW=(1560, 2240, 190, 600),
    TUBES=[(560, 240), (1300, 240), (2050, 240), (2800, 240), (3280, 240)],
    SPAWN=330, SOUTA=450, REN=540,
)
json.dump(L, open('out/layout5.json', 'w'))

c = Canvas(W, H, (26, 26, 24))

# ---------- parede ----------
c.grad(0, 0, W, 620, (60, 58, 50), (48, 47, 41), 0, tex=3, seed=4)
for x in range(LWALL, RWALL, 46):                                          # listras do papel de parede
    c.rect(x, 0, x + 2, 620, (38, 37, 32), alpha=0.35)
c.grad(0, 620, W, FLOOR, (44, 34, 26), (30, 24, 19), 0, tex=4, seed=5)        # lambri de madeira
for x in range(LWALL, RWALL, 120):
    c.rect(x, 620, x + 3, FLOOR, (14, 11, 9))
c.rect(0, 614, W, 626, (88, 70, 52), tex=3, seed=6)
c.rect(0, 614, W, 618, (140, 116, 88), alpha=0.5)
c.rect(0, 626, W, 632, (10, 8, 6), alpha=0.5)
# manchas de umidade
for x, y, r in ((420, 200, 120), (1180, 320, 90), (3120, 180, 140), (2680, 420, 80)):
    c.ellipse(x, y, r, r * 1.4, (30, 28, 22), alpha=0.5, blur=26)

# ---------- teto ----------
c.rect(0, 0, W, CEIL, (28, 28, 26), tex=3, seed=7)
for x in range(0, W, 120):
    c.rect(x, 0, x + 3, CEIL, (12, 12, 11))
for y in (38, 76):
    c.rect(0, y, W, y + 3, (12, 12, 11))
c.rect(0, CEIL - 5, W, CEIL, (10, 10, 9))
c.rect(0, CEIL, W, CEIL + 3, (110, 104, 90), alpha=0.25)
c.rect(1500, 0, 1640, CEIL, (6, 6, 6), alpha=0.8)       # placa de teto caída
c.poly([(1500, CEIL), (1560, CEIL + 22), (1640, CEIL)], (10, 10, 10), alpha=0.8)

# ---------- porta de entrada (fechada, de onde vieram) ----------
c.rect(160, 318, 310, FLOOR, (30, 24, 20), tex=3, seed=21)
c.rect(150, 306, 320, 322, (66, 52, 38))
c.rect(150, 306, 162, FLOOR, (66, 52, 38))
c.rect(308, 306, 320, FLOOR, (66, 52, 38))
c.rect(190, 400, 280, 520, (20, 16, 13))
c.rect(194, 404, 276, 516, (46, 52, 54), alpha=0.5)       # vidro fosco
c.ellipse(290, 600, 6, 6, (176, 150, 80))
c.text(235, 330, 'SAÍDA', 16, (150, 60, 50), anchor='ma', alpha=0.8)

# ---------- baias ----------
def monitor(x, y, on=False):
    c.rect(x, y, x + 74, y + 54, (14, 14, 14))
    c.rect(x + 4, y + 4, x + 70, y + 50, (28, 36, 40) if not on else (60, 100, 110))
    c.rect(x + 30, y + 54, x + 44, y + 70, (22, 22, 22))
    c.rect(x + 18, y + 70, x + 56, y + 76, (22, 22, 22))
    c.glow(x + 20, y + 14, 30, 22, (30, 40, 44), 0.8)
for i, px in enumerate((420, 800, 1160)):
    # divisória de tecido
    c.rect(px, 520, px + 18, FLOOR, (58, 62, 60), tex=4, seed=30 + i)
    c.rect(px, 520, px + 3, FLOOR, (110, 116, 112), alpha=0.35)
    c.rect(px - 2, 514, px + 20, 526, (36, 38, 36))
    # mesa
    c.rect(px + 18, 740, px + 340, 752, (84, 70, 54), tex=3, seed=40 + i)
    c.rect(px + 18, 740, px + 340, 744, (130, 112, 86), alpha=0.5)
    c.rect(px + 30, 752, px + 326, FLOOR, (30, 26, 22), alpha=0.9)
    c.rect(px + 40, 756, px + 140, 812, (44, 38, 32))
    c.rect(px + 120, 780, px + 132, 786, (150, 130, 80))
    monitor(px + 200, 656)
    c.rect(px + 60, 724, px + 84, 740, (200, 196, 180), alpha=0.8)           # caneca
    c.rect(px + 100, 728, px + 160, 740, (150, 144, 126), tex=2, seed=50 + i) # papéis
    # cadeira
    c.rect(px + 120, 772, px + 150, 832, (24, 24, 26))
    c.rect(px + 100, 800, px + 170, 812, (32, 32, 34))
    c.rect(px + 132, 812, px + 138, FLOOR - 6, (16, 16, 16))
    c.rect(px + 108, FLOOR - 8, px + 162, FLOOR - 2, (14, 14, 14))
# papéis espalhados
for x, y, w_, r_ in ((600, 838, 56, 6), (990, 840, 48, -5), (1130, 834, 60, 7), (1470, 842, 54, -4)):
    c.poly([(x, y), (x + w_, y - r_), (x + w_ - 6, y + 12), (x - 6, y + 14)], (150, 144, 124), alpha=0.75)

# ---------- janela atrás da mesa do chefe ----------
wx0, wx1, wy0, wy1 = L['WINDOW']
c.rect(wx0 - 14, wy0 - 14, wx1 + 14, wy1 + 14, (10, 9, 8))
c.grad(wx0, wy0, wx1, wy1, (12, 18, 30), (26, 30, 42), 0)
c.glow((wx0 + wx1) // 2 + 150, wy0 + 70, 200, 120, (40, 48, 70), 0.9)               # lua atrás das nuvens
rng = np.random.default_rng(5)
x = wx0
while x < wx1:                                                                       # prédios distantes
    bw = int(rng.integers(40, 110)); bh = int(rng.integers(90, 230))
    c.rect(x, wy1 - bh, min(x + bw, wx1), wy1, (10, 12, 18))
    for wy in range(wy1 - bh + 14, wy1 - 8, 22):
        for wx in range(x + 8, min(x + bw, wx1) - 8, 16):
            if rng.random() < 0.13:
                c.rect(wx, wy, wx + 6, wy + 9, (150, 112, 54) if rng.random() < 0.7 else (110, 130, 150), alpha=0.8)
    x += bw + int(rng.integers(0, 14))
c.rect(wx0, wy1 - 26, wx1, wy1, (8, 9, 12))
for xx in range(wx0, wx1, 34):                                                       # chuva (riscos parados)
    yy = int(rng.integers(wy0, wy1 - 80))
    c.line([(xx, yy), (xx - 6, yy + 38)], (90, 104, 130), 1, alpha=0.35)
# persiana quebrada
for y in range(wy0, wy0 + 150, 12):
    c.rect(wx0, y, wx1, y + 8, (74, 72, 62), tex=2, seed=y)
    c.rect(wx0, y + 8, wx1, y + 10, (12, 12, 11))
c.poly([(wx0 + 300, wy0 + 150), (wx0 + 420, wy0 + 150), (wx0 + 400, wy0 + 198), (wx0 + 330, wy0 + 176)], (74, 72, 62))
c.line([(wx0 + 340, wy0 + 150), (wx0 + 346, wy0 + 218)], (30, 30, 28), 2)
c.rect((wx0 + wx1) // 2 - 3, wy0, (wx0 + wx1) // 2 + 3, wy1, (10, 9, 8))               # montante
c.rect(wx0 - 20, wy1 + 14, wx1 + 20, wy1 + 30, (74, 62, 48), tex=3, seed=61)          # peitoril
c.rect(wx0 - 20, wy1 + 14, wx1 + 20, wy1 + 18, (130, 110, 84), alpha=0.5)
# luz fria da janela caindo no chão (por trás dos móveis)
c.poly([(wx0 + 40, wy1 + 30), (wx1 - 40, wy1 + 30), (wx1 + 360, FLOOR), (wx0 - 80, FLOOR)], (34, 48, 78), alpha=0.28, blur=8)

# ---------- mesa do chefe ----------
b0, b1, bt = L['BOSSDESK']
c.rect(b0, bt, b1, FLOOR, (40, 28, 22), tex=4, seed=71)
c.rect(b0 - 14, bt - 6, b1 + 14, bt + 18, (74, 52, 38), tex=4, seed=72)
c.rect(b0 - 14, bt - 6, b1 + 14, bt, (134, 102, 74), alpha=0.55)
c.rect(b0, bt + 18, b1, bt + 24, (8, 6, 5), alpha=0.5)
c.rect(b0 + 28, bt + 38, b0 + 150, FLOOR - 24, (52, 36, 28), tex=3, seed=73)           # lado vazio (cadeira)
for i in range(3):                                                                          # gavetas do lado direito
    y = bt + 40 + i * 36
    c.rect(b1 - 190, y, b1 - 30, y + 30, (56, 40, 30), tex=3, seed=74 + i)
    c.rect(b1 - 190, y, b1 - 30, y + 3, (110, 84, 62), alpha=0.5)
    c.rect(b1 - 124, y + 12, b1 - 96, y + 18, (170, 144, 84))
c.rect(b1 - 190, bt + 40 + 36, b1 - 30, bt + 40 + 36 + 30, (60, 44, 34))                   # gaveta do meio entreaberta
c.rect(b1 - 190, bt + 40 + 36 + 28, b1 - 30, bt + 40 + 36 + 34, (6, 5, 4))
# em cima da mesa
c.rect(b0 + 60, bt - 16, b0 + 190, bt - 6, (20, 20, 22))                                    # plaquinha
c.rect(b0 + 64, bt - 14, b0 + 186, bt - 8, (170, 150, 90), alpha=0.85)
c.text(b0 + 125, bt - 15, 'DIRETOR', 9, (30, 24, 14), anchor='ma')
c.rect(b0 + 230, bt - 44, b0 + 240, bt - 6, (30, 32, 30))                                   # luminária
c.line([(b0 + 235, bt - 44), (b0 + 262, bt - 70), (b0 + 296, bt - 60)], (36, 38, 36), 4)
c.poly([(b0 + 286, bt - 66), (b0 + 316, bt - 54), (b0 + 296, bt - 40)], (56, 58, 56))
c.rect(b0 + 330, bt - 20, b0 + 392, bt - 6, (150, 144, 124), tex=3, seed=78)               # papéis
c.rect(b0 + 336, bt - 26, b0 + 398, bt - 18, (134, 128, 110), tex=3, seed=79)
c.ellipse(b0 + 20, bt - 12, 12, 12, (0, 0, 0), alpha=0.0)
# café derramado (cena do crime)
c.ellipse(b0 + 270, bt + 6, 36, 5, (18, 12, 8), alpha=0.8)
c.rect(b0 + 120, bt - 24, b0 + 142, bt - 6, (176, 170, 150))

# cadeira de couro tombada
c.poly([(1945, FLOOR - 4), (2065, FLOOR - 60), (2091, FLOOR - 32), (1971, FLOOR + 2)], (30, 24, 22))
c.poly([(2059, FLOOR - 64), (2097, FLOOR - 118), (2204, FLOOR - 40), (2091, FLOOR - 28)], (24, 20, 18))
c.rect(2005, FLOOR - 4, 2049, FLOOR + 6, (12, 12, 12))
c.line([(1995, FLOOR - 16), (1975, FLOOR + 10)], (16, 16, 16), 5)
c.line([(2065, FLOOR - 20), (2085, FLOOR + 8)], (16, 16, 16), 5)

# ---------- arquivos ----------
for cid, label, x0, x1 in L['CABINETS']:
    top = 560
    c.rect(x0, top, x1, FLOOR, (74, 76, 68), tex=3, seed=x0)
    c.rect(x0, top, x0 + 4, FLOOR, (140, 142, 130), alpha=0.5)
    c.rect(x1 - 5, top, x1, FLOOR, (12, 12, 12), alpha=0.6)
    c.rect(x0, top, x1, top + 4, (150, 152, 140), alpha=0.5)
    for i in range(4):
        y = top + 12 + i * 70
        c.rect(x0 + 8, y, x1 - 8, y + 62, (62, 64, 58), tex=3, seed=x0 + i)
        c.rect(x0 + 8, y, x1 - 8, y + 3, (118, 120, 110), alpha=0.5)
        c.rect(x0 + 8, y + 59, x1 - 8, y + 62, (10, 10, 10), alpha=0.7)
        c.rect(x0 + 46, y + 22, x1 - 46, y + 28, (170, 160, 130))             # puxador
        c.rect(x0 + 22, y + 8, x1 - 22, y + 20, (200, 194, 170), alpha=0.9)   # porta-etiqueta
        c.text((x0 + x1) // 2, y + 8, label if i == 0 else '', 11, (36, 30, 24), anchor='ma')
# arquivo C meio aberto de levado
c.text(2875, 536, 'C', 28, (150, 140, 110), anchor='ma', alpha=0.0)

# bebedouro
c.rect(1460, 700, 1520, FLOOR, (96, 104, 106), tex=3, seed=81)
c.rect(1460, 700, 1464, FLOOR, (170, 180, 182), alpha=0.5)
c.ellipse(1490, 660, 26, 44, (60, 90, 100), alpha=0.7)
c.rect(1480, 726, 1500, 736, (16, 16, 16))

# quadro de avisos e quadros
c.rect(2380, 270, 2520, 380, (66, 46, 30), tex=3, seed=91)
c.rect(2388, 278, 2512, 372, (110, 84, 56), tex=3, seed=92)
for (px, py, pw, ph, rot) in ((2400, 290, 40, 52, 0), (2450, 296, 44, 40, 4), (2412, 336, 54, 30, -3), (2470, 340, 34, 28, 6)):
    c.rect(px, py, px + pw, py + ph, (172, 166, 148), tex=3, seed=px)
    c.ellipse(px + pw // 2, py + 4, 3, 3, (160, 40, 36))
c.rect(2640, 300, 2790, 420, (24, 20, 18))
c.rect(2646, 306, 2784, 414, (88, 90, 82), tex=3, seed=93)
c.text(2715, 330, 'METAS DO', 14, (46, 44, 38), anchor='ma')
c.text(2715, 348, 'TRIMESTRE', 14, (46, 44, 38), anchor='ma')
c.line([(2666, 380), (2700, 388), (2730, 372), (2766, 392)], (150, 44, 38), 3)
c.rect(3000, 280, 3100, 400, (24, 20, 18))
c.rect(3006, 286, 3094, 394, (60, 66, 70), tex=3, seed=94)
c.poly([(3006, 394), (3040, 330), (3070, 360), (3094, 320), (3094, 394)], (28, 32, 36))

# ---------- chão (carpete) ----------
c.grad(0, FLOOR, W, H, (36, 38, 42), (16, 17, 20), 0, tex=5, seed=101)
c.rect(0, FLOOR - 2, W, FLOOR + 6, (10, 8, 6), alpha=0.7)
c.rect(0, FLOOR - 14, W, FLOOR - 2, (68, 52, 38), tex=3, seed=102)                     # rodapé
for x in range(0, W, 90):
    c.rect(x, FLOOR + 8, x + 2, H, (12, 12, 14), alpha=0.5)
c.ellipse(1800, FLOOR + 50, 620, 16, (8, 9, 12), alpha=0.5, blur=6)

# ---------- corpo do chefe ----------
# O corpo e a cabeça viram sprites separados (corpo_chefe.png e cabeca_chefe.png): no jogo, quando o Kai passa por cima,
# os passos grudam no sangue e a cabeça se desprende. No fundo ficam só a poça de sangue e a luz da janela.
by = FLOOR - 2
def S(pts, k=1.55):
    return [(x, by - (by - y) * k) for x, y in pts]
# poça de sangue (escura, baixa)
c.ellipse(2430, by + 10, 200, 18, (30, 4, 4), alpha=0.95, blur=2)
c.ellipse(2420, by + 10, 158, 12, (74, 8, 8), alpha=0.9, blur=2)
c.ellipse(2380, by + 7, 72, 4, (160, 44, 40), alpha=0.35, blur=2)
c.ellipse(2500, by + 2, 34, 6, (60, 6, 6), alpha=0.85, blur=2)                     # sangue embaixo da cabeça

def draw_body(k, ox, oy):
    T = lambda pts: [(x - ox, y - oy) for x, y in pts]
    # pernas
    k.poly(T(S([(2180, by - 22), (2330, by - 34), (2340, by - 4), (2188, by - 2)])), (70, 74, 96))
    k.poly(T(S([(2200, by - 8), (2340, by - 6), (2346, by + 4), (2204, by + 6)])), (46, 48, 62))
    k.poly(T(S([(2150, by - 20), (2186, by - 24), (2190, by - 2), (2152, by)])), (62, 42, 30))             # sapatos
    k.poly(T(S([(2162, by - 2), (2200, by + 2), (2204, by + 8), (2164, by + 8)])), (46, 32, 24))
    # tronco (paletó)
    k.poly(T(S([(2320, by - 40), (2470, by - 46), (2476, by), (2326, by + 4)])), (52, 58, 80))
    k.poly(T(S([(2330, by - 34), (2380, by - 36), (2384, by - 10), (2336, by - 8)])), (214, 210, 198), alpha=0.9)  # camisa
    k.line(T([(2350, by - 52), (2402, by - 20)]), (130, 24, 24), 6)                                          # gravata
    k.poly(T([(2402, by - 46), (2432, by - 46), (2432, by - 24), (2402, by - 24)]), (30, 34, 50))             # bolso do paletó
    # braço estendido
    k.poly(T(S([(2440, by - 38), (2540, by - 20), (2540, by - 6), (2436, by - 20)])), (44, 50, 70))
    k.ellipse(2550 - ox, by - 18 - oy, 14, 10, (170, 138, 120))
    # pescoço cortado (some atrás da cabeça enquanto ela está no lugar)
    k.ellipse(2474 - ox, by - 30 - oy, 9, 12, (110, 20, 18))
    # luz fria da janela (contorno)
    k.line(T(S([(2326, by - 42), (2470, by - 48)])), (140, 160, 205), 2, alpha=0.6)
    k.line(T(S([(2182, by - 24), (2330, by - 36)])), (110, 130, 170), 2, alpha=0.5)

def draw_head(k, ox, oy):
    T = lambda pts: [(x - ox, y - oy) for x, y in pts]
    k.ellipse(2498 - ox, by - 34 - oy, 30, 28, (160, 128, 110))
    k.poly(T(S([(2474, by - 36), (2500, by - 46), (2522, by - 30), (2518, by - 24), (2476, by - 26)])), (76, 76, 82))
    k.ellipse(2498 - ox, by - 34 - oy, 30, 28, (30, 10, 10), alpha=0.3, blur=3)
    k.ellipse(2472 - ox, by - 30 - oy, 7, 14, (96, 14, 12), alpha=0.9)                # corte do pescoço
    k.line(T([(2482, by - 50), (2512, by - 52)]), (150, 166, 205), 2, alpha=0.45)

def sprite(drawfn, box, path):
    """Desenha a peça sobre preto e sobre branco: a diferença dá a transparência exata."""
    x0, y0, x1, y1 = box
    kb = Canvas(x1 - x0, y1 - y0, (0, 0, 0)); drawfn(kb, x0, y0)
    kw = Canvas(x1 - x0, y1 - y0, (255, 255, 255)); drawfn(kw, x0, y0)
    alpha = 1.0 - (kw.a - kb.a).mean(axis=2) / 255.0
    alpha = np.clip(alpha, 0, 1)
    rgb = np.where(alpha[..., None] > 0.003, kb.a / np.maximum(alpha[..., None], 0.003), 0)
    rgb = rgb * (0.94 + 0.06 * noise(y1 - y0, x1 - x0, 0.6, 77)[..., None] * 0.5) * np.array([0.78, 0.8, 0.86])  # mesma penumbra do fundo
    save_rgba(rgb, alpha, path)

BODY_BOX = (2140, by - 82, 2570, by + 14)
HEAD_BOX = (2460, by - 76, 2534, by - 2)
L['BODY_BOX'] = BODY_BOX
L['HEAD_BOX'] = HEAD_BOX
json.dump(L, open('out/layout5.json', 'w'))
sprite(draw_body, BODY_BOX, f'{OUT}/corpo_chefe.png')
sprite(draw_head, HEAD_BOX, f'{OUT}/cabeca_chefe.png')
# luz fria da janela sobre o lugar do corpo
c.glow(2370, by - 60, 260, 70, (50, 70, 112), 0.9)

# ---------- porta de saída (madeira com vidro fosco) ----------
d0, d1, dt, db = L['DOOR']
c.rect(d0 - 26, dt - 40, d1 + 26, db, (60, 46, 34), tex=3, seed=111)
c.rect(d0 - 26, dt - 40, d0 - 22, db, (140, 112, 84), alpha=0.35)
c.rect(d0 - 26, dt - 40, d1 + 26, dt - 34, (140, 112, 84), alpha=0.3)
c.rect(d0 - 8, dt - 8, d1 + 8, db, (6, 6, 6))
c.rect(d0 + 24, dt - 74, d1 - 24, dt - 50, (18, 18, 18))
c.text((d0 + d1) // 2, dt - 72, 'SAÍDA', 16, (170, 60, 50), anchor='ma')

# ---------- luzes ----------
for i, (tx, tw) in enumerate(L['TUBES']):
    c.rect(tx - tw // 2, 98, tx + tw // 2, 120, (18, 18, 17))
    c.rect(tx - tw // 2 + 6, 104, tx + tw // 2 - 6, 114, (196, 204, 190))
    c.cone(tx, 120, 720, tw * 0.9, tw * 3.0, (48, 52, 46), 1.0)
c.cone(1900, 190, 860, 600, 900, (14, 22, 38), 1.0)        # luz da janela

c.grime(seed=15, amount=9)
c.vignette(0.55)
c.finish(grain=2.2).save(f'{OUT}/cenario5_fundo.png')

# ---------- folhas da porta de madeira ----------
lw, lh = (d1 - d0) // 2, db - dt
def leaf(side):
    k = Canvas(lw, lh, (60, 46, 34))
    k.grad(0, 0, lw, lh, (78, 58, 42), (54, 40, 30), 0, tex=5, seed=121 + side)
    for y in range(0, lh, 3):
        if (y * 7 + side * 3) % 11 == 0:
            k.rect(0, y, lw, y + 1, (36, 26, 18), alpha=0.5)
    k.rect(0, 0, lw, 5, (140, 112, 84), alpha=0.4)
    k.rect(0 if side else lw - 8, 0, 8 if side else lw, lh, (18, 14, 10))
    k.rect(12, 40, lw - 16, 230, (30, 22, 16))                       # moldura do vidro
    k.rect(18, 46, lw - 22, 224, (74, 82, 82), tex=3, seed=130)      # vidro fosco
    k.rect(18, 46, lw - 22, 224, (10, 12, 14), alpha=0.35)
    k.rect(12, 300, lw - 16, 500, (66, 48, 34), tex=3, seed=131)     # almofada
    k.rect(12, 300, lw - 16, 304, (30, 22, 16))
    k.rect(12, 496, lw - 16, 500, (30, 22, 16))
    k.ellipse(lw - 22 if not side else 22, 330, 6, 6, (186, 156, 84))
    k.grime(seed=150 + side, amount=7, streaks=False)
    k.vignette(0.25)
    return k.finish(grain=1.6)
leaf(0).save(f'{OUT}/porta5_folha_esq.png')
leaf(1).save(f'{OUT}/porta5_folha_dir.png')
inter = Canvas(d1 - d0, db - dt, (8, 8, 9))
inter.grad(0, 0, d1 - d0, db - dt, (14, 14, 16), (4, 4, 5), 0)
inter.finish(grain=1.2).save(f'{OUT}/porta5_interior.png')

# ---------- gaveta de arquivo puxada ----------
gw, gh = 150, 84
gc = Canvas(gw, gh, (0, 0, 0))
gc.rect(6, 8, gw - 6, 38, (10, 9, 8))                                  # abertura escura
for i in range(7):                                                      # pastas
    gc.rect(12 + i * 18, 12, 28 + i * 18, 40, (150 - i * 4, 140 - i * 3, 112) if i % 2 else (110, 100, 80), tex=2, seed=i)
gc.rect(4, 36, gw - 4, gh - 4, (66, 68, 62), tex=3, seed=160)            # frente
gc.rect(4, 36, gw - 4, 40, (130, 132, 122), alpha=0.6)
gc.rect(gw // 2 - 24, 52, gw // 2 + 24, 58, (170, 160, 130))
mask = np.zeros((gh, gw), np.float32)
mask[6:gh - 2, 2:gw - 2] = 1.0
save_rgba(gc.a, mask, f'{OUT}/arquivo_gaveta.png')

# ---------- pistola (na mão do Souta) ----------
pw, ph = 80, 48
pc = Canvas(pw, ph, (0, 0, 0))
pc.poly([(4, 8), (62, 8), (66, 14), (66, 20), (30, 20), (26, 44), (12, 44), (14, 20), (4, 20)], (20, 21, 22))
pc.poly([(6, 9), (62, 9), (64, 12), (8, 12)], (92, 96, 98), alpha=0.8)
pc.rect(26, 22, 31, 26, (8, 8, 8))
pc.ellipse(36, 26, 8, 6, (10, 10, 10), alpha=0.6)
mask = np.zeros((ph, pw), np.float32)
from PIL import ImageDraw
mi = Image.new('L', (pw, ph), 0)
ImageDraw.Draw(mi).polygon([(4, 8), (62, 8), (66, 14), (66, 20), (30, 20), (26, 44), (12, 44), (14, 20), (4, 20)], fill=255)
save_rgba(pc.a, np.asarray(mi).astype(np.float32) / 255.0, f'{OUT}/pistola.png')
print('ok cenario5')
