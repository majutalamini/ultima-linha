"""Kai no mundo real: a mesma roupa, encardida. Mexe só nas cores da paleta (cada cor limpa vira uma cor suja), então
a sujeira é igual em todos os quadros e não pisca na animação. O trem limpo da Abertura usa a paleta original."""
import numpy as np

LUM = np.array([0.3, 0.59, 0.11])
ENCARDIDO = np.array([138.0, 122.0, 100.0])  # camisa branca que virou bege-acinzentado
MANCHA = np.array([124.0, 106.0, 88.0])      # manchas marrons
POEIRA = np.array([96.0, 88.0, 80.0])        # poeira por cima da calça, do cabelo e dos sapatos


def suja(pal, forca=1.0):
    """forca 1 = o Kai depois do trem; menos que 1 = só um pouco encardido."""
    pal = np.asarray(pal, float)
    lum = pal @ LUM
    pele = (pal[:, 0] > pal[:, 1] + 12) & (pal[:, 0] > pal[:, 2] + 20) & (lum > 110)
    # tons claros vizinhos (em ordem de brilho) alternam entre encardido e mancha: como a camisa é desenhada com
    # muitas dobras, isso vira um tecido manchado que acompanha o desenho em todos os quadros
    ordem = np.argsort(np.argsort(lum))
    claro = np.where(ordem % 2 == 0, 0, 1)[:, None]
    alvo_claro = np.where(claro, MANCHA, ENCARDIDO)
    k = np.where(lum > 150, 0.6, np.where(lum > 90, 0.4, 0.2))
    k = np.where(pele, 0.15, k)  # a pele só perde um pouco da cor
    alvo = np.where(lum[:, None] > 90, alvo_claro, POEIRA)
    escurece = np.where(pele, 0.97, np.where(lum > 150, 0.86, 0.94))
    k, escurece = k * forca, 1 - (1 - escurece) * forca
    out = (pal + (alvo - pal) * k[:, None]) * escurece[:, None]
    return np.clip(np.round(out), 0, 255).astype(int)


if __name__ == "__main__":
    p = np.array([[235, 230, 225], [120, 110, 105], [40, 35, 40], [220, 170, 150]])
    s = suja(p)
    assert (s[0] < p[0]).all() and s.shape == p.shape  # a camisa escurece
    assert abs(int(s[3, 0]) - 220) < 30                 # a pele quase não muda
    print(s)
