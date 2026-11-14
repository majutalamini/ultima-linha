"""Cenário 4 na grade dos personagens (piloto da reestruturação visual).
A arte antiga (Ferramentas/Cenarios45/cenario4.py, 100 px/u, filtro bilinear) fica intacta em Assets/Art/Cenario4; esta
versão vai para Assets/Art/Cenario4/Pixel:
- reduzida para 58 px/u (0,58x): um pixel do fundo = um pixel do Kai; o tamanho em unidades não muda, então colisores,
  alavancas e portas continuam nos mesmos lugares;
- tirada do verde-concreto e presa na paleta oficial (Ferramentas/Assets/paleta.py), com um dither leve nos degradês;
- sujeira por cima: escorridos de ferrugem embaixo dos canos e manchas de umidade no rodapé;
- mapa de normais do fundo (relevo pela luminância), para as luzes 2D pegarem nos canos, painéis e na parede.
Uso: python Ferramentas/Cenario4/c4_pixel.py"""
import os
import sys

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "Assets"))
import paleta  # noqa: E402

ORIG = os.path.join(RAIZ, "Assets", "Art", "Cenario4")
SAIDA = os.path.join(ORIG, "Pixel")
ESC = 0.58
PECAS = ["gaveta_aberta", "porta4_folha_esq", "porta4_folha_dir", "porta4_interior"]
# acentos que a paleta da estação não tem e que são pista ou leitura: vermelho da luz e dos ponteiros, creme do mostrador,
# madeira da mesa (o vermelho é o mesmo das letras do HUD, B8322A)
ACENTOS = np.array([[184, 50, 42], [120, 28, 24], [70, 18, 16], [214, 204, 184], [176, 166, 148], [92, 66, 44], [62, 44, 30],
                    [12, 10, 10]], np.uint8)


def reduz(img):
    w, h = img.size
    return img.resize((round(w * ESC), round(h * ESC)), Image.LANCZOS)  # BOX borrava os ponteiros do relógio (04:17)


def gradua(rgb):
    """Do verde-concreto para os cinzas frios e marrons da estação: tira saturação e puxa um pouco para o quente escuro."""
    a = rgb.astype(np.float32)
    cinza = a.mean(2, keepdims=True)
    a = cinza + (a - cinza) * 0.6
    a = a * np.array([1.04, 0.98, 0.92]) * 0.82
    return a.clip(0, 255)


def sujeira(rgb, semente=4):
    """Escorridos verticais de ferrugem/umidade, mais fortes no alto da parede e no rodapé (chão a 850 px na arte antiga)."""
    rng = np.random.default_rng(semente)
    h, w = rgb.shape[:2]
    a = rgb.astype(np.float32)
    for _ in range(w // 14):
        x = int(rng.integers(0, w))
        y0 = int(rng.integers(int(h * 0.08), int(h * 0.45)))
        comp = int(rng.integers(20, 120))
        larg = int(rng.integers(1, 3))
        forca = float(rng.uniform(0.05, 0.16))
        perfil = np.linspace(1.0, 0.0, comp)[:, None, None]
        y1 = min(h, y0 + comp)
        a[y0:y1, x:x + larg] *= 1 - forca * perfil[:y1 - y0]
        a[y0:y1, x:x + larg, 0] += 10 * forca * perfil[:y1 - y0, :, 0]  # um fio de ferrugem
    piso = int(850 * ESC)
    faixa = np.linspace(0.0, 1.0, 30)[:, None, None]
    a[piso - 30:piso] *= 1 - 0.18 * faixa  # umidade subindo do chão
    return a.clip(0, 255)


def normais(rgb, forca=2.5):
    lum = rgb.astype(np.float32).mean(2) / 255.0
    gx = np.zeros_like(lum)
    gy = np.zeros_like(lum)
    gx[:, 1:-1] = lum[:, 2:] - lum[:, :-2]
    gy[1:-1] = lum[:-2] - lum[2:]  # y da imagem cresce para baixo
    n = np.dstack([-gx * forca, -gy * forca, np.ones_like(lum)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)


def main():
    os.makedirs(SAIDA, exist_ok=True)
    pal = np.concatenate([paleta.cores(), ACENTOS])
    fundo = reduz(Image.open(os.path.join(ORIG, "cenario4_fundo.png")).convert("RGB"))
    rgb = paleta.prende(sujeira(gradua(np.array(fundo))), pal, dither=0.6)
    assert rgb.shape[:2] == (546, 1972), rgb.shape
    Image.fromarray(rgb, "RGB").save(os.path.join(SAIDA, "cenario4_fundo.png"))
    Image.fromarray(normais(rgb), "RGB").save(os.path.join(SAIDA, "cenario4_fundo_normal.png"))
    for nome in PECAS:
        img = reduz(Image.open(os.path.join(ORIG, nome + ".png")).convert("RGBA"))
        a = np.array(img)
        a[..., :3] = paleta.prende(gradua(a[..., :3]), pal)
        a[..., 3] = np.where(a[..., 3] > 127, 255, 0)  # pixel art: transparência de 1 bit
        Image.fromarray(a, "RGBA").save(os.path.join(SAIDA, nome + ".png"))
    print("->", SAIDA)


if __name__ == "__main__":
    main()
