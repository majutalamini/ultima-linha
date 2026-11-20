"""Peças de profundidade do Cenário 3 (grade de 58 px, paleta escura da estação):
- portao_vao.png: o corredor de azulejos atrás da grade do portão (antes era um degradê preto que não era de lugar nenhum).
- porta_sombra.png: a sombra que o batente da porta da administração projeta na parede e no chão (porta recuada).
Uso: python Ferramentas/Cenario3/profundidade.py"""
import os
import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
OBJ = os.path.join(os.path.dirname(os.path.dirname(AQUI)), "Assets", "Art", "Cenario3", "Objetos")
ESCURO, PISO, BORDA = np.array([9, 9, 10]), np.array([34, 31, 28]), np.array([58, 52, 45])


def vao(w=136, h=236):
    """Corredor visto de frente: preto em cima, chão de ladrilho sumindo no fundo, batente interno claro à esquerda
    (a luz da plataforma entra pela esquerda) e escuro à direita."""
    a = np.zeros((h, w, 4), np.uint8)
    a[..., :3] = ESCURO
    a[..., 3] = 255
    piso0 = int(h * 0.8)
    for y in range(piso0, h):                      # chão: clareia para perto, com juntas de ladrilho
        k = (y - piso0) / (h - piso0)
        cor = ESCURO + (PISO - ESCURO) * k
        a[y, :, :3] = cor
        if (y - piso0) % 6 == 5:
            a[y, :, :3] = cor * 0.7
    for x in range(0, w, 11):
        a[piso0:, x, :3] = a[piso0:, x, :3] * 0.75
    a[:, :6, :3] = BORDA * 0.8                     # batente interno (espessura da parede)
    a[:, 6:8, :3] = BORDA * 0.45
    a[:, -5:, :3] = ESCURO * 0.6
    a[:8, :, :3] = ESCURO * 0.5                    # verga
    return a


SAGUAO = os.path.join(OBJ, "parede_saguao.png")


def corredor(w=136, h=236, x0=430):
    """O que se vê por um vão aberto: o mesmo prédio, um corredor de azulejos indo para o escuro. Recorta a parede do
    saguão (arte do usuário, mesma grade) na altura certa (chão do vão = chão do saguão, linha 545), escurece para o fundo
    e para cima e põe a espessura da parede nas bordas."""
    s = np.array(Image.open(SAGUAO).convert("RGB")).astype(np.float32)
    pe = 545
    rec = s[pe - h:pe, x0:x0 + w].copy()
    yy, xx = np.mgrid[0:h, 0:w]
    meio = 1 - np.abs(xx - w / 2) / (w / 2)                       # o fundo do corredor fica no meio
    fundo = 1 - meio * 0.45
    alto = 0.6 + 0.4 * (yy / h)                                   # o teto do corredor some no escuro
    rec *= (fundo * alto)[..., None] * 1.15
    piso = int(h * 0.86)
    rec[piso:, :] = s[pe - 4:pe - 4 + (h - piso), x0:x0 + w] * 1.0   # o chão do saguão continua para dentro
    rec[:, :6] = BORDA * 0.8                                      # espessura da parede (luz de fora à esquerda)
    rec[:, 6:8] = BORDA * 0.45
    rec[:, -5:] = ESCURO * 0.6
    rec[:6] = ESCURO * 0.5                                        # verga
    return np.dstack([rec.clip(0, 255), np.full((h, w), 255)]).astype(np.uint8)


def sombra(w=171, h=300, dx=10, dy=8):
    """Recorte do batente (155x288) deslocado para a direita e para baixo, em preto translúcido, com borda suave."""
    a = np.zeros((h, w, 4), np.uint8)
    a[dy:dy + 288 - 4, dx:dx + 155, 3] = 120
    for i in range(4):                             # borda de 4 px mais fraca
        al = 30 * (i + 1)
        a[dy + i, dx + i:dx + 155 - i, 3] = np.minimum(a[dy + i, dx + i:dx + 155 - i, 3], al)
        a[dy:dy + 284, dx + 155 - 1 - i, 3] = np.minimum(a[dy:dy + 284, dx + 155 - 1 - i, 3], al)
    a[-12:, :, 3] = np.linspace(110, 0, 12, dtype=np.uint8)[:, None]  # mancha no chão, na frente
    return a


if __name__ == "__main__":
    for nome, img in (("portao_vao", corredor()), ("porta_sombra", sombra())):
        assert img[..., 3].any()
        Image.fromarray(img, "RGBA").save(os.path.join(OBJ, nome + ".png"))
        print(nome, img.shape)
