# Gaveta aberta dos arquivos do cenário 5, no mesmo traço dos arquivos pintados no fundo.
# A frente da gaveta é recortada do próprio painel (a 3ª gaveta do arquivo DEMISSÕES), então
# aberta ela tem exatamente a mesma cor, puxador e plaquinha de quando está fechada.
# A gaveta "sai" alguns pixels para baixo (vem na direção da câmera) e por cima dela aparecem
# as laterais de metal e as pastas suspensas. Sai em 58 ppu, como os painéis.
import os
import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.normpath(os.path.join(AQUI, "..", ".."))
ART = os.path.join(RAIZ, "Assets", "Art", "Cenarios4a7")

# 3ª gaveta do arquivo DEMISSÕES no painel b (coordenadas do painel b, 58 ppu)
FRENTE = (1779 - 1251, 440, 1867 - 1251, 493)
SAI = 10          # quantos pixels a gaveta vem para frente
ABERTURA = 2      # vão escuro no alto, onde a gaveta saiu do móvel


def main():
    pb = np.array(Image.open(os.path.join(ART, "c5_painel_b.png")).convert("RGBA"))
    x0, y0, x1, y1 = FRENTE
    frente = pb[y0:y1, x0:x1].copy()
    fh, fw = frente.shape[:2]
    h = fh + SAI + 2
    s = np.zeros((h, fw, 4), np.uint8)

    # vão escuro: o buraco do móvel atrás da gaveta
    s[0:SAI + 1, :] = (7, 5, 4, 255)

    # caixa da gaveta vista de cima: laterais de metal e fundo escuro
    topo = ABERTURA
    caixa = (24, 21, 16, 255)
    s[topo:SAI + 1, 1:fw - 1] = (12, 10, 8, 255)
    s[topo:SAI + 1, 1:4] = caixa
    s[topo:SAI + 1, fw - 4:fw - 1] = caixa
    s[topo, 1:4] = (45, 40, 33, 255)
    s[topo, fw - 4:fw - 1] = (45, 40, 33, 255)

    # pastas suspensas: tons de papel pardo apagados, com as abinhas em alturas diferentes
    rng = np.random.default_rng(7)
    papeis = [(86, 72, 48), (74, 62, 42), (96, 84, 60), (68, 58, 40), (104, 96, 80)]
    x = 5
    i = 0
    while x < fw - 6:
        larg = int(rng.integers(3, 6))
        cor = papeis[i % len(papeis)]
        alto = topo + 3 + int(rng.integers(0, 3))
        s[alto:SAI + 1, x:x + larg] = (*cor, 255)
        s[alto, x:x + larg] = tuple(min(255, c + 18) for c in cor) + (255,)
        s[alto:SAI + 1, x + larg - 1] = tuple(int(c * 0.6) for c in cor) + (255,)   # divisória entre pastas
        if i % 3 == 1:                                                                  # abinha
            s[alto - 2:alto, x + 1:x + 3] = tuple(min(255, c + 10) for c in cor) + (255,)
        x += larg
        i += 1

    # frente da gaveta, igual à fechada, um pouco mais abaixo
    s[SAI:SAI + fh] = frente
    # linha de luz no topo da frente (a quina que agora fica para fora)
    s[SAI, 1:fw - 1] = (52, 46, 38, 255)
    # sombra da gaveta no móvel logo abaixo
    s[SAI + fh:SAI + fh + 2, 1:fw - 1] = (0, 0, 0, 140)

    # duas peças para a animação: o miolo (vão + pastas, que "cresce" para baixo a partir do topo da gaveta)
    # e a frente (com a sombra), que desliza SAI pixels para baixo. As duas têm o pivô no topo.
    dentro = s[0:SAI + 1].copy()
    frente_img = np.zeros((fh + 2, fw, 4), np.uint8)
    frente_img[:fh] = frente
    frente_img[0, 1:fw - 1] = (52, 46, 38, 255)
    frente_img[fh:fh + 2, 1:fw - 1] = (0, 0, 0, 140)
    Image.fromarray(dentro, "RGBA").save(os.path.join(ART, "gaveta_dentro.png"))
    Image.fromarray(frente_img, "RGBA").save(os.path.join(ART, "gaveta_frente.png"))
    if os.environ.get("PREVIA"):  # pasta opcional para salvar a gaveta montada, só para conferir
        Image.fromarray(s, "RGBA").save(os.path.join(os.environ["PREVIA"], "gaveta_aberta_previa.png"))
    print(f"gaveta_dentro {fw}x{SAI + 1}  gaveta_frente {fw}x{fh + 2}  a frente desce {SAI} px ao abrir")


if __name__ == "__main__":
    main()
