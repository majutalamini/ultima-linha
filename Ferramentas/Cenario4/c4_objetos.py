"""Objetos provisórios do Cenário 4 em pixel art, na grade de 58 px e na paleta oficial (até chegarem as artes geradas).
A parede, o teto e o chão do C4 são a parede do saguão do C3 (arte do usuário); estes são os móveis que ficam na frente.
Tamanhos batem com os colisores do Cenario4SceneBuilder (medidas em px da arte antiga x 0,58).
Grava em Assets/Art/Cenario4/Pixel. Uso: python Ferramentas/Cenario4/c4_objetos.py"""
import math
import os
import shutil
import sys

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "Assets"))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "Cenario3"))
import paleta  # noqa: E402
import profundidade  # noqa: E402

SAIDA = os.path.join(RAIZ, "Assets", "Art", "Cenario4", "Pixel")
OBJ3 = os.path.join(RAIZ, "Assets", "Art", "Cenario3", "Objetos")
ACENTOS = np.array([[184, 50, 42], [120, 28, 24], [214, 204, 184], [176, 166, 148], [92, 66, 44], [62, 44, 30],
                    [150, 120, 40], [96, 78, 30]], np.uint8)
PAL = np.concatenate([paleta.cores(), ACENTOS])
RNG = np.random.default_rng(11)

CONTORNO = (14, 12, 11)
METAL = [(30, 30, 30), (46, 45, 42), (64, 62, 57), (86, 82, 74)]       # escuro -> claro
MADEIRA = [(34, 25, 18), (52, 38, 27), (70, 52, 36), (92, 68, 46)]
FERRUGEM = (88, 46, 30)


def tela(w, h):
    return np.zeros((h, w, 4), np.uint8)


def caixa(a, x0, y0, x1, y1, tons, borda=True, luz_topo=True):
    """Bloco com contorno escuro, face média, luz no topo/esquerda e sombra embaixo/direita (luz fria vem de cima)."""
    a[y0:y1, x0:x1, :3] = tons[1]
    a[y0:y1, x0:x1, 3] = 255
    if luz_topo:
        a[y0:y0 + 2, x0:x1, :3] = tons[3]
        a[y0:y1, x0:x0 + 1, :3] = tons[2]
    a[y1 - 3:y1, x0:x1, :3] = tons[0]
    a[y0:y1, x1 - 2:x1, :3] = tons[0]
    if borda:
        a[y0, x0:x1, :3] = CONTORNO
        a[y1 - 1, x0:x1, :3] = CONTORNO
        a[y0:y1, x0, :3] = CONTORNO
        a[y0:y1, x1 - 1, :3] = CONTORNO


def suja(a, forca=0.18, ferrugem=0.04):
    """Manchas e escorridos (a mesma sujeira dos objetos dos C1-C3), só onde há pixel."""
    h, w = a.shape[:2]
    m = a[..., 3] > 0
    ruido = RNG.random((h, w))
    a[..., :3] = np.where((m & (ruido < 0.03))[..., None], (a[..., :3] * (1 - forca)).astype(np.uint8), a[..., :3])  # pouco: pixel solto vira chiado
    for _ in range(max(1, w // 10)):
        x = int(RNG.integers(1, max(2, w - 1)))
        y0 = int(RNG.integers(0, max(1, h // 2)))
        comp = int(RNG.integers(4, max(5, h // 2)))
        sel = m[y0:y0 + comp, x]
        a[y0:y0 + comp, x, :3][sel] = (a[y0:y0 + comp, x, :3][sel] * 0.8).astype(np.uint8)
    pontos = m & (RNG.random((h, w)) < ferrugem * 0.25)
    a[pontos, :3] = FERRUGEM


def fecha(a):
    """Paleta oficial, transparência de 1 bit."""
    a[..., :3] = paleta.prende(a[..., :3], PAL)
    a[..., 3] = np.where(a[..., 3] > 127, 255, 0)
    return a


def relogio(r=31):
    """Relógio de parede parado em 04:17 (a pista da gaveta): os ponteiros têm de ler de longe."""
    d = 2 * r + 2
    a = tela(d, d)
    yy, xx = np.mgrid[0:d, 0:d]
    dist = np.hypot(xx - r, yy - r)
    a[dist <= r] = (*CONTORNO, 255)
    a[dist <= r - 2] = (*METAL[2], 255)
    a[dist <= r - 4] = (206, 196, 176, 255)
    a[(dist <= r - 4) & (yy > r + 6) & (xx > r)] = (176, 166, 148, 255)  # sombra do aro no mostrador
    for k in range(12):
        ang = math.radians(k * 30)
        for t in range(r - 9, r - 5 + (2 if k % 3 == 0 else 0)):
            x, y = int(round(r + math.sin(ang) * t)), int(round(r - math.cos(ang) * t))
            a[y, x, :3] = CONTORNO
            if k % 3 == 0:
                a[y, x + 1, :3] = CONTORNO

    def ponteiro(ang_graus, comp, larg, cor):
        ang = math.radians(ang_graus)
        for t in np.linspace(0, comp, comp * 3):
            x, y = r + math.sin(ang) * t, r - math.cos(ang) * t
            for dx in range(-(larg // 2), larg - larg // 2):
                for dy in range(-(larg // 2), larg - larg // 2):
                    a[int(round(y + dy)), int(round(x + dx)), :3] = cor
    ponteiro((4 + 17 / 60) * 30, int(r * 0.5), 3, CONTORNO)  # horas
    ponteiro(17 * 6, int(r * 0.78), 2, CONTORNO)             # minutos
    ponteiro(17 * 6 + 180, int(r * 0.2), 1, (184, 50, 42))
    a[r - 1:r + 2, r - 1:r + 2, :3] = (184, 50, 42)
    racha = [(r - 14 + i, r - 18 + int(2 * math.sin(i))) for i in range(12)]  # vidro rachado
    for x, y in racha:
        a[y, x, :3] = (236, 232, 220)
    return fecha(a)


def bilhete():
    a = tela(28, 36)
    a[3:36, 1:27] = (198, 188, 166, 255)
    a[3:36, 26:27] = (150, 140, 120, 255)
    a[35, 1:27] = (150, 140, 120, 255)
    for y in range(9, 33, 4):
        a[y, 5:5 + int(RNG.integers(12, 19)), :3] = (70, 66, 60)
    a[0:4, 12:16] = (184, 50, 42, 255)  # alfinete vermelho
    suja(a, 0.1, 0.0)
    return fecha(a)


def mesa(w=197, h=93):
    """Mesa de supervisor, de metal com tampo de madeira; gaveta com cadeado à esquerda (a da chave)."""
    a = tela(w, h)
    caixa(a, 0, 0, w, 9, MADEIRA)                     # tampo
    caixa(a, 6, 9, 96, h, METAL)                      # gaveteiro esquerdo (com a gaveta da chave)
    caixa(a, w - 60, 9, w - 6, h, METAL)              # gaveteiro direito
    a[9:30, 96:w - 60] = (*METAL[0], 255)             # vão das pernas, escuro
    for gy in (14, 42, 68):
        caixa(a, 12, gy, 90, gy + 22, METAL)
        a[gy + 9:gy + 12, 44:58, :3] = METAL[3]       # puxador
    a[18:26, 47:55, :3] = (150, 120, 40)              # cadeado de combinação (latão)
    a[21:23, 49:53, :3] = CONTORNO
    for gy in (14, 50):
        caixa(a, w - 56, gy, w - 10, gy + 30, METAL)
    a[2:7, 30:58, :3] = (198, 188, 166)               # papéis em cima
    a[0:3, 140:156, :3] = (40, 40, 40)                # telefone velho
    suja(a)
    return fecha(a)


def gaveta_aberta():
    a = tela(84, 26)
    caixa(a, 0, 4, 84, 26, METAL)
    a[4:10, 4:80, :3] = (20, 18, 16)                  # o fundo da gaveta, aberto
    a[5:9, 10:40, :3] = (198, 188, 166)               # papéis velhos
    return fecha(a)


def armarios(n=4, w=36, h=150):
    a = tela(n * w + 2, h)
    for i in range(n):
        x0 = i * w
        caixa(a, x0, 0, x0 + w + 2, h, [(30, 34, 32), (46, 52, 48), (62, 68, 62), (82, 88, 80)])
        for y in range(12, 34, 5):
            a[y:y + 2, x0 + 8:x0 + w - 8, :3] = (20, 22, 20)      # venezianas
        a[70:84, x0 + w - 9:x0 + w - 6, :3] = (110, 108, 98)      # puxador
        a[h - 30:h - 28, x0 + 4:x0 + w - 2, :3] = (34, 38, 36)
    a[40:150, 2 * w + 1:3 * w, :3] = (24, 26, 24)                 # um armário entreaberto
    suja(a, 0.2, 0.06)
    return fecha(a)


def caixote(w=93, h=75):
    a = tela(w, h)
    caixa(a, 0, 0, w, h, MADEIRA)
    for y in range(10, h - 4, 14):
        a[y, 2:w - 2, :3] = MADEIRA[0]                            # tábuas
    for x in (4, w - 10):
        caixa(a, x, 2, x + 7, h - 2, MADEIRA, borda=False)        # travessas
    for i in range(min(w, h) - 6):                               # diagonal
        x, y = 6 + int(i * (w - 12) / (h - 6)), 3 + i
        if y < h - 3:
            a[y, x:x + 4, :3] = MADEIRA[2]
    a[30:40, 28:66, :3] = (120, 108, 70)                          # estêncil MANUT. apagado
    a[32:38, 30:64, :3] = MADEIRA[1]
    suja(a, 0.2, 0.02)
    return fecha(a)


def passarela(w=406, alto=151):
    """Passarela de grade metálica com guarda-corpo, em pernas até o chão. O deque fica no topo da perna (alto px)."""
    h = alto + 52
    a = tela(w, h)
    deck = h - alto
    caixa(a, 0, deck - 8, w, deck + 2, METAL)                     # deque
    for x in range(3, w - 3, 4):
        a[deck - 6:deck, x, :3] = METAL[0]                        # grade
    for x in (6, w // 2 - 4, w - 14):                             # pernas
        caixa(a, x, deck + 2, x + 9, h, METAL)
        for y in range(deck + 14, h - 10, 26):                    # contraventamento
            for i in range(10):
                if x + 9 + i * 2 < w:
                    a[y + i, x + 9 + i:x + 11 + i, :3] = METAL[1]
    a[deck - 46:deck - 42, 0:w] = (*METAL[2], 255)                # corrimão
    a[deck - 46, 0:w] = (*METAL[3], 255)
    a[deck - 26:deck - 24, 0:w] = (*METAL[1], 255)
    for x in range(0, w, 58):                                     # montantes
        a[deck - 46:deck - 8, x:x + 3, :3] = METAL[2]
        a[deck - 46:deck - 8, x:x + 3, 3] = 255
    a[deck + 2:deck + 6, 0:w, :3] = (40, 34, 14)                  # faixa de aviso embaixo do deque
    a[deck + 2:deck + 6, 0:w, 3] = 255
    for x in range(0, w, 12):
        a[deck + 2:deck + 6, x:x + 6, :3] = (150, 120, 40)
    suja(a, 0.15, 0.05)
    return fecha(a)


def gerador(w=145, h=122):
    a = tela(w, h)
    caixa(a, 0, 14, w, h, [(30, 36, 32), (46, 54, 46), (64, 72, 62), (84, 92, 80)])
    caixa(a, 10, 0, w - 30, 16, METAL)                            # tampa do motor
    for y in range(30, 70, 6):
        a[y:y + 3, 12:70, :3] = (20, 24, 20)                      # aletas
    caixa(a, 86, 30, 128, 66, METAL)                              # mostrador
    yy, xx = np.mgrid[0:36, 0:42]
    face = np.hypot(xx - 21, yy - 18) < 13
    a[30:66, 86:128][face] = (206, 196, 176, 255)
    a[42:44, 107:117, :3] = (184, 50, 42)                         # ponteiro
    a[h - 14:h - 8, :, :3] = (40, 34, 14)
    for x in range(0, w, 12):
        a[h - 14:h - 8, x:x + 6, :3] = (150, 120, 40)             # faixa de aviso
    a[70:h - 16, w - 14:w - 8, :3] = (20, 20, 20)                 # cabo grosso
    suja(a, 0.2, 0.05)
    return fecha(a)


def painel_energia(w=74, h=104):
    """Painel de energia com quatro encaixes de alavanca; o quarto vazio, com fios soltos (onde saem as faíscas)."""
    a = tela(w, h)
    caixa(a, 0, 0, w, h, METAL)
    caixa(a, 6, 6, w - 6, 22, METAL)
    a[10:18, 12:62, :3] = (20, 18, 16)                            # visor apagado
    for i in range(4):
        x = 9 + i * 15
        caixa(a, x, 32, x + 11, 76, METAL)
        a[36:72, x + 4:x + 7, :3] = (14, 12, 11)                  # fenda do encaixe
    a[74:96, 57:59, :3] = (184, 50, 42)                           # fios soltos do quarto encaixe
    a[74:94, 61:63, :3] = (150, 120, 40)
    a[h - 12:h - 6, 8:w - 8, :3] = (40, 34, 14)
    suja(a)
    return fecha(a)


def luz_emergencia():
    a = tela(16, 12)
    caixa(a, 0, 0, 16, 5, METAL)
    a[5:11, 2:14] = (120, 28, 24, 255)
    a[6:9, 4:12, :3] = (184, 50, 42)
    a[11, 2:14] = (*CONTORNO, 255)
    return fecha(a)


def salva(img, nome):
    Image.fromarray(img, "RGBA").save(os.path.join(SAIDA, nome + ".png"))


def main():
    os.makedirs(SAIDA, exist_ok=True)
    for velho in ("cenario4_fundo", "cenario4_fundo_normal", "porta4_folha_esq", "porta4_folha_dir", "porta4_interior"):
        for ext in (".png", ".png.meta"):
            p = os.path.join(SAIDA, velho + ext)
            if os.path.exists(p):
                os.remove(p)
    pecas = {"relogio": relogio(), "bilhete": bilhete(), "mesa": mesa(), "gaveta_aberta": gaveta_aberta(),
             "armarios": armarios(), "caixote": caixote(), "passarela": passarela(), "gerador": gerador(),
             "painel_energia": painel_energia(), "luz_emergencia": luz_emergencia()}
    for nome, img in pecas.items():
        assert img[..., 3].any(), nome
        salva(img, nome)
    # a porta de serviço é a porta da administração do C3 (mesma estação); o vão é o corredor de azulejos
    for nome in ("porta_adm", "porta_adm_folha_esq", "porta_adm_folha_dir", "porta_sombra"):
        shutil.copy(os.path.join(OBJ3, nome + ".png"), os.path.join(SAIDA, nome.replace("porta_adm", "porta4") + ".png"))
    salva(profundidade.corredor(132, 248), "porta4_interior")
    print("->", SAIDA, ", ".join(pecas))


if __name__ == "__main__":
    main()
