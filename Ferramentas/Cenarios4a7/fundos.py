"""Fundos dos Cenários 4 a 7 a partir dos painéis gerados pelo usuário (Ferramentas/Cenarios4a7/referencias, PROMPTS.md).
Mesmo tratamento das paredes do C3 (Ferramentas/Cenario3/objetos_arte.py, paredes()):
- reduz para 540 px de altura (BOX): a mesma escala da estação, 1 pixel do fundo = 1 pixel do Kai;
- brilho levado ao da parede de estacao.png e paleta curta tirada do próprio painel (48 cores), para ficar no tom dos C0-C3
  sem perder o verde dos armários ou o carpete vinho do escritório;
- relógios repintados em 04:17 (a pista do C4 e a hora do C7);
- teto espelhado escurecendo em cima (110 px), como as paredes do C3.
Grava Assets/Art/Cenarios4a7/<painel>.png e medidas.json (largura de cada painel). Uso: python Ferramentas/Cenarios4a7/fundos.py"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "PixelArt"))
import extrai  # noqa: E402
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "Cenario3"))
import profundidade  # noqa: E402

REF = os.path.join(AQUI, "referencias")
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Cenarios4a7")
ESTACAO = os.path.join(RAIZ, "Assets", "Art", "Cenario2", "Objetos", "estacao.png")
TELA, TETO, CORES = 540, 110, 48
# o C4 vem 85 %: os móveis pintados estavam grandes demais ao lado dos personagens (porta 1,3x o Kai, agora 1,1x)
ESCALA = {"c4_painel_a": 0.85, "c4_painel_b": 0.85}
PAINEIS = ["c4_painel_a", "c4_painel_b", "c5_painel_a", "c5_painel_b", "c6_inicio", "c6_meio", "c6_fim", "c7_painel_b"]
# relógios (no painel original): centro x, y, raio do mostrador
RELOGIOS = {"c4_painel_a": (1296, 248, 72), "c7_painel_b": (815, 207, 56)}
# vãos de porta vazios pintados nos painéis (px do painel já reduzido, sem o teto): x0, x1, y0, y1 -> folhas e corredor
PORTAS = {"porta_c4": (1066, 650, 768, 0, 233), "porta_c5": (1251, 790, 924, 278 + 110, 550 + 110)}
PORTA_REF = os.path.join(RAIZ, "Ferramentas", "Cenario3", "referencias", "objetos", "porta_administracao.png")
PORTA_FOLHAS = (95, 442, 790, 335, 1640)  # as folhas na referência da porta do C3 (Ferramentas/Cenario3/objetos_arte.py)
CREME, PRETO, VERMELHO = np.array([200, 190, 168]), np.array([24, 20, 18]), np.array([150, 40, 32])


def relogio_0417(a, cx, cy, r):
    """Apaga os ponteiros pintando o mostrador por cima (só o miolo, longe dos traços das horas) e desenha 04:17."""
    yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
    d = np.hypot(xx - cx, yy - cy)
    face = a[(d < r * 0.95) & (d > r * 0.7)].reshape(-1, 3)
    cor_face = np.median(face, 0) if len(face) else CREME
    a[d < r * 0.7] = cor_face

    def ponteiro(graus, comp, larg, cor):
        ang = math.radians(graus)
        for t in np.linspace(0, comp, int(comp * 2)):
            x, y = cx + math.sin(ang) * t, cy - math.cos(ang) * t
            a[(np.hypot(xx - x, yy - y) <= larg / 2)] = cor
    ponteiro((4 + 17 / 60) * 30, r * 0.5, r * 0.11, PRETO)
    ponteiro(17 * 6, r * 0.68, r * 0.07, PRETO)
    a[d <= r * 0.08] = VERMELHO
    return a


def folhas():
    """As folhas da porta da administração do C3 refeitas no tamanho de cada vão (sem esticar a grade: reduz da referência)
    e o corredor de azulejos que aparece quando elas abrem."""
    ref = np.array(Image.open(PORTA_REF).convert("RGB")).astype(float)
    x0, xm, x1, y0, y1 = PORTA_FOLHAS
    for nome, (_, px0, px1, py0, py1) in PORTAS.items():
        w, h = px1 - px0, py1 - py0
        for lado, (a, b) in (("esq", (x0, xm)), ("dir", (xm, x1))):
            f = np.array(Image.fromarray(ref[y0:y1, a:b].astype(np.uint8)).resize((w // 2, h), Image.BOX)).astype(float) * 0.62
            pal = extrai.paleta(f.reshape(-1, 3), 24)
            f = pal[extrai.indexa(f, pal)]
            Image.fromarray(np.dstack([f, np.full((h, w // 2), 255)]).clip(0, 255).astype(np.uint8), "RGBA").save(
                os.path.join(SAIDA, f"{nome}_folha_{lado}.png"))
        Image.fromarray(profundidade.corredor(w // 2 * 2, h), "RGBA").save(os.path.join(SAIDA, f"{nome}_interior.png"))


# a pega em T da alavanca pintada no painel de tração de c6_inicio (px do painel já reduzido, com o teto): x0, x1, y0, y1.
# Ela vira um sprite à parte (desce no trilho quando o Kai encaixa a alavanca) e o lugar dela é refeito com a linha logo
# abaixo (moldura + trilho escuro, que continuam iguais para baixo).
PEGA_C6 = (333, 357, 371, 380)


def recorta_pega(out):
    x0, x1, y0, y1 = PEGA_C6
    Image.fromarray(out[y0:y1, x0:x1].copy(), "RGBA").save(os.path.join(SAIDA, "alavanca_c6.png"))
    out[y0:y1, x0:x1] = out[y1 + 1, x0:x1]
    return out


def com_teto(rgb, teto=TETO):
    topo = rgb[:teto][::-1].astype(float)
    k = np.linspace(0.06, 0.7, teto)[:, None, None]
    return np.concatenate([topo * k, rgb.astype(float)], 0)


def main():
    os.makedirs(SAIDA, exist_ok=True)
    e = np.array(Image.open(ESTACAO).convert("RGB")).astype(float)
    lum_e = np.median(e[100:370] @ extrai.LUM)
    medidas = {}
    for nome in PAINEIS:
        im = np.array(Image.open(os.path.join(REF, nome + ".png")).convert("RGB")).astype(float)
        if nome in RELOGIOS:
            im = relogio_0417(im, *RELOGIOS[nome])
        h, w = im.shape[:2]
        esc = ESCALA.get(nome, 1.0)
        tela = round(TELA * esc)
        W = round(w * tela / h)
        a = np.array(Image.fromarray(im.clip(0, 255).astype(np.uint8)).resize((W, tela), Image.BOX)).astype(float)
        a = a[:, 3:-3]  # as bordas dos painéis gerados têm 1-2 px de moldura clara
        a *= lum_e / max(1.0, np.median(a[int(60 * esc):int(330 * esc)] @ extrai.LUM))
        pal = extrai.paleta(a.reshape(-1, 3), CORES)
        a = pal[extrai.indexa(a, pal)]
        out = np.dstack([com_teto(a, TELA + TETO - tela), np.full((TELA + TETO, a.shape[1]), 255)]).clip(0, 255).astype(np.uint8)
        if nome == "c6_inicio":
            out = recorta_pega(out)
        Image.fromarray(out, "RGBA").save(os.path.join(SAIDA, nome + ".png"))
        medidas[nome] = {"w": int(out.shape[1]), "h": int(out.shape[0]), "escala": tela / h}
        print(nome, out.shape[1], "x", out.shape[0])
    folhas()
    with open(os.path.join(SAIDA, "medidas.json"), "w") as f:
        json.dump(medidas, f, indent=1)


if __name__ == "__main__":
    main()
