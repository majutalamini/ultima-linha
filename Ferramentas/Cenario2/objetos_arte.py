"""Objetos do Cenário 2 (leituras, puzzles, engate, estação). python objetos_arte.py

As imagens do gerador ficam em referencias/objetos/, em fundo verde puro (a estação não: ocupa a imagem toda). Cada peça é
recortada do verde e reduzida para a grade dos personagens (58 px por unidade), como Ferramentas/Cenario1/objetos_arte.py,
de onde vêm as funções. Saem em Assets/Art/Cenario2/Objetos/.
- caixa_fusiveis_cheia não veio do gerador: é a vazia com o fusível encaixado nos três buracos e a luzinha verde acesa.
- previa_objetos.png (aqui mesmo): todas as peças lado a lado, com o Kai para comparar o tamanho.
"""
import os
import sys

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "Cenario1"))
import objetos_arte as oa  # noqa: E402

oa.REF = os.path.join(AQUI, "referencias", "objetos")
oa.OUT = os.path.join(RAIZ, "Assets", "Art", "Cenario2", "Objetos")

# largura (px na grade de 58) de cada peça; a altura sai da proporção do desenho
LARGURA = {
    "relogio": 36, "celular": 30, "cadeado_rodas": 40, "mapa_linha": 130, "aviso_operadora": 64,
    "fusivel": 22, "bolsa_cracha": 66, "presente": 44, "remedio": 22, "engate": 151,
}
CAIXA_ALTURA = 56
# os três encaixes da caixa vazia (px da imagem do gerador): x0, x1 de cada um e a faixa y onde o fusível fica
ENCAIXES = [(345, 462), (487, 604), (628, 745)]
ENCAIXE_Y = (600, 840)
LED = (548, 300)  # luzinha verde na faixa de cima


def por_largura(nome, tom=0.6, cores=20):
    rgb, m = oa.recorta(nome)
    x0, y0, x1, y1 = oa.pecas(m, 1)[0]
    alt = max(4, round(LARGURA[nome] * (y1 - y0) / (x1 - x0)))
    return oa.peca(rgb, m, (x0, y0, x1, y1), alt, nome, tom=tom, cores=cores)[0]


def caixas():
    """Vazia e cheia com o mesmo recorte e a mesma escala, para trocar uma pela outra no lugar."""
    rgb, m = oa.recorta("caixa_fusiveis_vazia")
    caixa = oa.pecas(m, 1)[0]
    oa.peca(rgb, m, caixa, CAIXA_ALTURA, "caixa_fusiveis_vazia", tom=0.6, cores=24)

    frgb, fm = oa.recorta("fusivel")
    fx0, fy0, fx1, fy1 = oa.pecas(fm, 1)[0]
    fus = np.dstack([frgb[fy0:fy1, fx0:fx1], fm[fy0:fy1, fx0:fx1] * 255]).astype(np.uint8)
    fus = Image.fromarray(fus, "RGBA").rotate(90, expand=True)
    cheia = Image.fromarray(np.dstack([rgb, m * 255]).astype(np.uint8), "RGBA")
    for x0, x1 in ENCAIXES:
        w = int((x1 - x0) * 0.62)
        h = int((ENCAIXE_Y[1] - ENCAIXE_Y[0]) * 0.92)
        f = fus.resize((w, h), Image.LANCZOS)
        cheia.alpha_composite(f, ((x0 + x1) // 2 - w // 2, ENCAIXE_Y[0] + (ENCAIXE_Y[1] - ENCAIXE_Y[0] - h) // 2))
    a = np.array(cheia).astype(float)
    lx, ly = LED
    yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
    d = np.hypot(xx - lx, yy - ly)
    led = d < 22
    a[led, :3] = [90, 255, 120]
    halo = (d >= 22) & (d < 60) & (a[:, :, 3] > 0)
    k = (1 - (d[halo] - 22) / 38)[:, None] * 0.55
    a[halo, :3] = a[halo, :3] * (1 - k) + np.array([90, 255, 120]) * k
    oa.peca(a[:, :, :3], a[:, :, 3] > 127, caixa, CAIXA_ALTURA, "caixa_fusiveis_cheia", tom=0.6, cores=24)


def estacao():
    """A estação vista da janela/da porta do fim: imagem inteira, altura da tela (540 px), um pouco escurecida."""
    im = Image.open(os.path.join(oa.REF, "estacao_janela.png")).convert("RGB")
    w = round(im.width * 540 / im.height)
    a = np.array(im.resize((w, 540), Image.BOX)).astype(float)
    pal = oa.extrai.paleta(a.reshape(-1, 3), 40)
    a = pal[oa.extrai.indexa(a, pal)] * 0.75
    out = np.zeros((540, w, 4), np.uint8)
    out[:, :, :3] = np.clip(a, 0, 255)
    out[:, :, 3] = 255
    oa.salva(out, "estacao")


def vagao_curto():
    """O vagao_antigo sem o trecho do meio: emenda no meio da 1ª porta lateral com o meio da 2ª (o vagão se repete a
    cada 650 px), então sai com dois bancos em vez de três. Cenario2SceneBuilder tira o mesmo trecho das medidas."""
    v = Image.open(oa.VAGAO).convert("RGB")
    a, b = EMENDA
    out = Image.new("RGB", (v.width - (b - a), v.height))
    out.paste(v.crop((0, 0, a, v.height)), (0, 0))
    out.paste(v.crop((b, 0, v.width, v.height)), (a, 0))
    out.save(os.path.join(RAIZ, "Assets", "Art", "Cenario2", "vagao_curto.png"))


def engate_largo():
    """O engate mais comprido (ENGATE_LARGO px): repete uma fatia lisa da barra de cada lado do acoplamento do meio, sem
    esticar os pixels. Correndo, o Kai pula ~5 u: o engate tem que caber esse pouso (Cenario2SceneBuilder.Buraco2)."""
    e = np.array(Image.open(os.path.join(oa.OUT, "engate.png")))
    w = e.shape[1]
    falta = ENGATE_LARGO - w
    esq, dir_ = falta // 2, falta - falta // 2
    fat_e, fat_d = e[:, 29:30], e[:, w - 30:w - 29]  # uma coluna só de barra (sem suporte, mangueira nem braçadeira)
    rep = lambda f, n: np.concatenate([f] * (n // f.shape[1] + 1), 1)[:, :n]
    out = np.concatenate([e[:, :32], rep(fat_e, esq), e[:, 32:w - 32], rep(fat_d, dir_), e[:, w - 32:]], 1)
    oa.salva(out.astype(np.uint8), "engate")


ENGATE_LARGO = 228
EMENDA = (616, 1266)  # px do vagao_antigo: centro da 1ª e da 2ª porta lateral


def previa():
    nomes = list(LARGURA) + ["caixa_fusiveis_vazia", "caixa_fusiveis_cheia"]
    ims = [Image.open(os.path.join(oa.OUT, n + ".png")) for n in nomes]
    kai = Image.open(oa.KAI).convert("RGBA")
    ims.insert(0, kai)
    W = sum(i.width for i in ims) + 12 * len(ims)
    H = max(i.height for i in ims) + 10
    tela = Image.new("RGBA", (W, H), (24, 22, 22, 255))
    x = 6
    for i in ims:
        tela.alpha_composite(i, (x, H - 5 - i.height))
        x += i.width + 12
    tela = tela.resize((W * 3, H * 3), Image.NEAREST)
    tela.save(os.path.join(AQUI, "previa_objetos.png"))


def main():
    os.makedirs(oa.OUT, exist_ok=True)
    for n in LARGURA:
        por_largura(n, tom=0.95 if n == "engate" else 0.6)  # o engate é onde se pousa: tem que aparecer no escuro do vão
    caixas()
    estacao()
    vagao_curto()
    engate_largo()
    previa()
    print("ok:", sorted(os.listdir(oa.OUT)))


if __name__ == "__main__":
    main()
