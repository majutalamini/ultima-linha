"""Botões, alavancas, portas de ponta e o túnel entre os vagões (Cenários 0 e 1). python objetos_arte.py

As imagens do gerador ficam em referencias/objetos/, em fundo verde puro. Cada peça é recortada do verde, reduzida para a
grade dos personagens (58 px por unidade, a mesma do vagao_antigo) e presa numa paleta curta. Saem em
Assets/Art/Cenario1/Objetos/ (a Unity importa a 58 px por unidade já descontando a escala do vagão: Cenario1SceneBuilder).
- alavanca_base / alavanca_cabo, alavanca_emperrada, lampada / lampada_suporte, sinal_porta, painel_emergencia
- antepara (ponta direita do vagão; a esquerda é a mesma espelhada), porta_ponta (a folha que fecha o vão da antepara)
- tunel (fundo do vão entre os vagões), mostrador / ponteiro (minijogo da alavanca emperrada, na interface)
- medidas.json: onde fica a porta, o pilar e a plataforma na antepara, e o eixo do ponteiro (a Unity lê daqui)
- previa_objetos.png (aqui mesmo): o fim do vagão do Cenário 0, o vão e o começo do vagão do Cenário 1, com o Kai.
"""
import json
import os
import sys

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "PixelArt"))
import extrai  # noqa: E402
import fatia  # noqa: E402

REF = os.path.join(AQUI, "referencias", "objetos")
OUT = os.path.join(RAIZ, "Assets", "Art", "Cenario1", "Objetos")
VAGAO = os.path.join(RAIZ, "Assets", "Art", "Cenario0", "vagao_antigo.png")
KAI = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt", "Kai", "idle_00.png")

ALTURA_KAI = 222                 # px do Kai na grade (Ferramentas/Cenario0/abertura_arte.py)
PORTA = 250                      # vão da porta de ponta: ~1,13x o Kai (as laterais têm 1,15x); o teto dela cai no preto
PISO_ANTEPARA = 1125             # na imagem da antepara: linha onde os pés pisam (fundo do chão e beirada da grade)
PILAR_ANTEPARA = 545             # na imagem da antepara: onde termina o pilar da ponta (daí para fora é a plataforma)
FUNDO_X = 2075 / 4230            # px do vagao_antigo por px do fundo original (LevelWidth do Cenário 1)
CHAO_VAGAO = 540 - round((1101 - (870 + 160)) * 540 / 1101)  # FloorPy no vagao_antigo (linha dos pés)
TOM = 0.8                        # objetos ficam um pouco mais claros que o vagão (dá para achar) mas no clima dele


def recorta(nome):
    """RGB e máscara do que não é verde."""
    rgb = np.array(Image.open(os.path.join(REF, nome + ".png")).convert("RGB"))
    return rgb.astype(float), fatia.mascara(rgb)


def pecas(m, n):
    """As n maiores peças da imagem, da esquerda para a direita: (x0, y0, x1, y1)."""
    _, caixas = fatia.componentes(m)
    caixas = sorted(caixas, key=lambda c: -c[4])[:n]
    return sorted([tuple(int(v) for v in c[:4]) for c in caixas], key=lambda c: c[0])


def tira_verde(rgb, m):
    """O verde que vaza nas bordas vira o tom do vizinho (g limitado pela média de r e b)."""
    out = rgb.copy()
    g_max = (rgb[:, :, 0] + rgb[:, :, 2]) / 2 + 18
    out[:, :, 1] = np.where(m, np.minimum(rgb[:, :, 1], g_max), rgb[:, :, 1])
    return out


def reduz(rgb, m, escala, cores=20, tom=TOM):
    """Média de cada bloco (só dos pixels do desenho) e paleta curta. Devolve RGBA."""
    h, w = m.shape
    W, H = max(1, round(w * escala)), max(1, round(h * escala))
    rgb = tira_verde(rgb, m)
    peso = Image.fromarray((m * 255).astype(np.uint8)).resize((W, H), Image.BOX)
    soma = np.stack([np.array(Image.fromarray((rgb[:, :, c] * m).astype(np.float32), "F").resize((W, H), Image.BOX))
                     for c in range(3)], 2)
    a = np.array(peso).astype(float) / 255
    cor = soma / np.maximum(a[:, :, None], 1e-3)
    cheio = a > 0.5
    px = cor[cheio]
    pal = extrai.paleta(px, min(cores, max(2, len(px) // 4)))
    cor = pal[extrai.indexa(cor, pal)] * tom
    out = np.zeros((H, W, 4), np.uint8)
    out[:, :, :3] = np.clip(cor, 0, 255)
    out[:, :, 3] = cheio * 255
    return out


def salva(a, nome):
    Image.fromarray(a, "RGBA").save(os.path.join(OUT, nome + ".png"))
    return a


def peca(rgb, m, caixa, altura, nome, **kw):
    x0, y0, x1, y1 = caixa
    esc = altura / (y1 - y0)
    return salva(reduz(rgb[y0:y1, x0:x1], m[y0:y1, x0:x1], esc, **kw), nome), esc


def buraco(m):
    """O vão da porta: o maior 'verde' que não encosta na borda da imagem (x0, y0, x1, y1)."""
    _, caixas = fatia.componentes(~m & ~extrai.fora(m))
    return tuple(int(v) for v in max(caixas, key=lambda c: c[4])[:4])


def no_vagao(a, cores=40):
    """Leva a peça para a luz e a paleta do vagao_antigo: mesma luminosidade mediana, cores que existem nele."""
    v = np.array(Image.open(VAGAO).convert("RGB")).astype(float)[120:500].reshape(-1, 3)
    lum_v = np.median(v @ extrai.LUM)
    cheio = a[:, :, 3] > 0
    rgb = a[:, :, :3].astype(float)
    rgb *= 1.35 * lum_v / max(1.0, np.median(rgb[cheio] @ extrai.LUM))  # um pouco acima: a mediana do vagão inclui o escuro
    pal = extrai.paleta(v[(v @ extrai.LUM) > 4], cores)
    viva = rgb[cheio][rgb[cheio].max(1) - rgb[cheio].min(1) > 40]  # tinta vermelha descascada: guarda um pouco dela
    if len(viva) > 50:
        pal = np.vstack([pal, extrai.paleta(viva, 4)])
    a = a.copy()
    a[:, :, :3] = np.clip(pal[extrai.indexa(rgb, pal)], 0, 255)
    return a


def main():
    os.makedirs(OUT, exist_ok=True)
    med = {}

    rgb, m = recorta("alavanca")
    base, cabo = pecas(m, 2)
    peca(rgb, m, base, 30, "alavanca_base")
    a, esc = peca(rgb, m, cabo, 50, "alavanca_cabo")
    med["cabo_eixo"] = 1 - 0.09  # o eixo é o anel de baixo: ~9% da altura a partir da base

    rgb, m = recorta("alavanca_emperrada")
    peca(rgb, m, pecas(m, 1)[0], 46, "alavanca_emperrada")

    rgb, m = recorta("lampada")
    bulbo, suporte = pecas(m, 2)
    _, esc = peca(rgb, m, bulbo, 20, "lampada", tom=1.0)
    peca(rgb, m, suporte, max(4, round((suporte[3] - suporte[1]) * esc)), "lampada_suporte")

    rgb, m = recorta("sinal_porta")
    peca(rgb, m, pecas(m, 1)[0], 10, "sinal_porta")

    rgb, m = recorta("painel_emergencia")
    peca(rgb, m, pecas(m, 1)[0], 50, "painel_emergencia")

    # antepara: a escala sai da porta (o vão transparente no meio da parede)
    rgb, m = recorta("antepara")
    bx0, by0, bx1, by1 = buraco(m)
    ys, xs = np.nonzero(m)
    x0, y0, x1, y1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    esc = PORTA / (by1 - by0)
    ant = salva(no_vagao(reduz(rgb[y0:y1, x0:x1], m[y0:y1, x0:x1], esc, cores=32, tom=1.0)), "antepara")
    H, W = ant.shape[:2]
    # colunas e linhas de referência, medidas na própria imagem (px da imagem do gerador)
    pilar = PILAR_ANTEPARA
    med["antepara"] = {
        "w": W, "h": H,
        "porta": [round((bx0 - x0) * esc), round((by0 - y0) * esc), round((bx1 - x0) * esc), round((by1 - y0) * esc)],
        "pilar_fim": round((pilar - x0) * esc),
        "chao": round((PISO_ANTEPARA - y0) * esc),  # linha dos pés, dentro e na plataforma
        "plataforma_fim": round((x1 - x0) * esc),
    }

    rgb, m = recorta("porta_ponta")
    pp = med["antepara"]["porta"]
    x0, y0, x1, y1 = pecas(m, 1)[0]
    folha = no_vagao(reduz(rgb[y0:y1, x0:x1], m[y0:y1, x0:x1], (pp[3] - pp[1]) / (y1 - y0), cores=24, tom=1.0))
    # a porta gerada é mais larga que o vão da antepara: estreita para caber (com 2 px de batente de cada lado)
    salva(np.array(Image.fromarray(folha, "RGBA").resize((pp[2] - pp[0] + 4, pp[3] - pp[1]), Image.NEAREST)), "porta_ponta")

    rgb, m = recorta("tunel")
    x0, y0, x1, y1 = pecas(m, 1)[0]
    salva(reduz(rgb[y0 + 2:y1 - 2, x0 + 2:x1 - 2], m[y0 + 2:y1 - 2, x0 + 2:x1 - 2], 640 / (y1 - y0 - 4), cores=32, tom=0.45), "tunel")

    # interface: maior, sem escurecer (fica sobre um véu escuro)
    rgb, m = recorta("mostrador")
    disco, ponteiro = pecas(m, 2)
    _, esc = peca(rgb, m, disco, 160, "mostrador", tom=0.9, cores=28)
    x0, y0, x1, y1 = ponteiro
    pt = salva(reduz(rgb[y0:y1, x0:x1], m[y0:y1, x0:x1], esc, cores=12, tom=0.9), "ponteiro")
    # eixo do ponteiro: o centro do anel (canto de baixo à esquerda); a ponta aponta para cima e à direita
    med["ponteiro"] = {"w": pt.shape[1], "h": pt.shape[0], "eixo": [round(0.19 * pt.shape[1]), round(0.836 * pt.shape[0])]}

    with open(os.path.join(OUT, "medidas.json"), "w") as f:
        json.dump(med, f, indent=1)
    print(json.dumps(med, indent=1))
    previa(med)


def cola(tela, im, x, y_base):
    """Cola im com a base em y_base (px da tela)."""
    tela.alpha_composite(im, (int(x), int(y_base - im.height)))


def previa(med):
    """Fim do vagão do C0 | vão | começo do vagão do C1, montados como a Unity monta (Cenario1SceneBuilder.BuildVao)."""
    vag = Image.open(VAGAO).convert("RGBA")
    ant = Image.open(os.path.join(OUT, "antepara.png"))
    a = med["antepara"]
    fim = round(4128 * FUNDO_X)       # RightWallPx: a antepara do C0 começa aqui
    esq = round(225 * FUNDO_X)        # LeftWallPx: a antepara do C1 termina aqui
    vao = 255                         # 4,4 unidades entre as plataformas
    x0 = fim - 600                    # recorte da prévia
    tela = Image.new("RGBA", (600 + 2 * a["w"] + vao + 600, 540), (0, 0, 0, 255))
    cola(tela, Image.open(os.path.join(OUT, "tunel.png")), 0, 540 + 150)
    tela.alpha_composite(vag.crop((x0, 0, fim, 540)), (0, 0))
    topo = CHAO_VAGAO - a["chao"]
    tela.alpha_composite(ant, (600, topo))
    x_c1 = 600 + 2 * a["w"] + vao     # onde o vagão do C1 tem o LeftWallPx
    tela.alpha_composite(vag.crop((0, 0, 600 + esq, 540)), (x_c1 - esq, 0))
    tela.alpha_composite(ant.transpose(Image.FLIP_LEFT_RIGHT), (x_c1 - a["w"], topo))
    folha = Image.open(os.path.join(OUT, "porta_ponta.png"))
    tela.alpha_composite(folha, (x_c1 - a["porta"][2] - 2, topo + a["porta"][1]))
    kai = Image.open(KAI)
    pe = np.nonzero(np.array(kai)[:, :, 3])[0].max() + 1
    tela.alpha_composite(kai, (600 + a["pilar_fim"] + 10 - 128, CHAO_VAGAO - pe))
    tela.alpha_composite(kai, (400 - 128, CHAO_VAGAO - pe))
    tela.alpha_composite(Image.open(os.path.join(OUT, "alavanca_base.png")), (250, CHAO_VAGAO - 30))
    tela.alpha_composite(Image.open(os.path.join(OUT, "painel_emergencia.png")), (560, CHAO_VAGAO - 170))
    tela.convert("RGB").save(os.path.join(AQUI, "previa_objetos.png"))


if __name__ == "__main__":
    main()
