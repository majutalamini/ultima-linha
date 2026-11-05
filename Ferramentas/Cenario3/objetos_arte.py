"""Objetos e paredes do Cenário 3 (a estação). python objetos_arte.py

As imagens do gerador ficam em referencias/objetos/, em fundo verde puro (o quadro de partidas veio em fundo preto; as
paredes ocupam a imagem toda). Cada peça é recortada e reduzida para a grade dos personagens (58 px por unidade) com as
funções de Ferramentas/Cenario1/objetos_arte.py. Saem em Assets/Art/Cenario3/Objetos/, com medidas.json (px na grade, a
partir do canto de baixo à esquerda de cada peça) para o Cenario3SceneBuilder.
- Paredes: altura da tela (540 px), cortadas do meio de uma coluna ao meio da outra para emendar, nas cores de
  Assets/Art/Cenario2/Objetos/estacao.png (a estação que se vê pela porta do fim do Cenário 2). estacao_inicio é essa
  mesma imagem, cortada no meio da coluna da direita: a primeira parede do cenário é a que o jogador já viu.
- Montadas aqui: folhas e vão escuro da porta da administração, grade e batente do portão (a grade corre), painel com a
  lâmpada verde, mezanino sem a escada (sobe-se pelos caixotes).
- Em cima de cada parede, TETO px das vigas espelhadas e escurecidas: na câmera do jogo a parede não chega ao alto da tela.
- previa_objetos.png (aqui mesmo): as peças em pé na parede da estação, com o Kai do lado.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "Cenario1"))
import objetos_arte as oa  # noqa: E402

oa.REF = os.path.join(AQUI, "referencias", "objetos")
oa.OUT = os.path.join(RAIZ, "Assets", "Art", "Cenario3", "Objetos")
ESTACAO = os.path.join(RAIZ, "Assets", "Art", "Cenario2", "Objetos", "estacao.png")
TELA = 540
CHAO_PAREDE = 400  # linha dos pés nas paredes (de cima, na altura da tela): a faixa tátil da plataforma
TOM = 0.62

# altura (px na grade) de cada peça simples; o Kai tem 222 px
ALTURA = {
    "cartaz_clinica": 104, "quadro_partidas": 92, "achados_perdidos": 92, "maquina_automatica": 226,
    "bilheteria": 262, "bilhete": 14, "moeda": 12,
}
# peças montadas: altura de uma parte conhecida (px do gerador -> px na grade)
PORTA_BATENTE = (265, 1645, 262)   # batente da porta da administração: y0, y1 e altura na grade (~1,18x o Kai)
PORTA_FOLHAS = (95, 442, 790, 335, 1640)  # x0, x do meio, x1, y0, y1 das folhas
PORTAO = (165, 1385, 262)           # trilho até o pé do portão
PORTAO_GRADE = (135, 752, 295, 1385)
PAINEL = (240, 1170, 78)            # só a caixa (sem lâmpada e cabos)
PAINEL_LAMPADA = (420, 665, 15, 240)
MEZANINO = (540, 955, 226)          # chão do mezanino até o pé das colunas: passa da cabeça do Kai
MEZANINO_CORTE = (365, 112)         # x0 (sem a escada), y0 (sem o ar-condicionado do teto)
MEZANINO_DECK = (365, 1465, 540)    # x0, x1 e y do chão do mezanino
MEZANINO_MONITOR = (1140, 1285, 245, 355)
CAIXOTES = (640, 965, 70)           # topo do caixote baixo até o chão: 1,2 u
CAIXOTES_DEGRAUS = [(160, 752, 640), (760, 1290, 200)]  # x0, x1, topo de cada degrau
MAQUINA_BANDEJA = (165, 605, 1290, 1450)
MAQUINA_MOEDA = (510, 750, 970, 1115)
BILHETERIA_PAPEL = (258, 395, 480, 620)

medidas = {}


def caixa_de(nome, m):
    return oa.pecas(m, 1)[0]


def mede(nome, caixa, esc, **pontos):
    """Guarda tamanho e pontos (px do gerador) já na grade, a partir do canto de baixo à esquerda da peça."""
    x0, y0, x1, y1 = caixa
    d = {"w": round((x1 - x0) * esc), "h": round((y1 - y0) * esc)}
    for k, v in pontos.items():
        if len(v) == 2:
            d[k] = [round((v[0] - x0) * esc, 1), round((y1 - v[1]) * esc, 1)]
        else:  # x0, x1, y0, y1 -> centro e tamanho
            a, b, c, e = v
            d[k] = [round(((a + b) / 2 - x0) * esc, 1), round((y1 - (c + e) / 2) * esc, 1), round((b - a) * esc, 1), round((e - c) * esc, 1)]
    medidas[nome] = d


def simples(nome):
    rgb, m = oa.recorta(nome)
    c = caixa_de(nome, m)
    a, esc = oa.peca(rgb, m, c, ALTURA[nome], nome, tom=TOM, cores=24)
    return rgb, m, c, esc


def verde_no_preto(nome):
    """O quadro de partidas veio em fundo preto: o preto ligado à borda vira verde (o resto do desenho também é escuro)."""
    im = Image.open(os.path.join(oa.REF, nome + ".png")).convert("RGB")
    for xy in [(0, 0), (im.width - 1, 0), (0, im.height - 1), (im.width - 1, im.height - 1), (im.width // 2, im.height - 1)]:
        ImageDraw.floodfill(im, xy, (0, 255, 0), thresh=22)
    rgb = np.array(im).astype(float)
    return rgb, oa.fatia.mascara(rgb.astype(np.uint8))


def por_parte(nome, rgb, m, caixa, y0, y1, alt, saida=None, cores=24):
    esc = alt / (y1 - y0)
    x0, cy0, x1, cy1 = caixa
    h = round((cy1 - cy0) * esc)
    return oa.peca(rgb, m, caixa, h, saida or nome, tom=TOM, cores=cores)[0], esc


def porta():
    rgb, m = oa.recorta("porta_administracao")
    c = caixa_de("porta", m)
    fx0, fxm, fx1, fy0, fy1 = PORTA_FOLHAS
    # o vão: o mesmo desenho com as folhas apagadas para um escuro com uma luz fria no alto (sala lá dentro)
    vao = rgb.copy()
    yy = np.arange(fy0, fy1)[:, None]
    escuro = 10 + 18 * np.exp(-((yy - fy0) / 260.0))
    vao[fy0:fy1, fx0:fx1] = np.repeat(escuro, fx1 - fx0, 1)[:, :, None] * np.array([0.9, 1.0, 1.1])
    _, esc = por_parte("porta", vao, m, c, PORTA_BATENTE[0], PORTA_BATENTE[1], PORTA_BATENTE[2], "porta_adm")
    for nome, a, b in (("porta_adm_folha_esq", fx0, fxm), ("porta_adm_folha_dir", fxm, fx1)):
        mm = np.zeros_like(m)
        mm[fy0:fy1, a:b] = True
        oa.peca(rgb, mm, (a, fy0, b, fy1), round((fy1 - fy0) * esc), nome, tom=TOM, cores=24)
    mede("porta_adm", c, esc, folhas=(fx0, fx1, fy0, fy1), folha_esq=(fx0, fxm, fy0, fy1), folha_dir=(fxm, fx1, fy0, fy1), chao=(0, 1645))


def portao():
    rgb, m = oa.recorta("portao_servico")
    c = caixa_de("portao", m)
    gx0, gx1, gy0, gy1 = PORTAO_GRADE
    grade = np.zeros_like(m)
    grade[gy0:gy1, gx0:gx1] = m[gy0:gy1, gx0:gx1]
    _, esc = por_parte("portao_batente", rgb, m & ~grade, c, PORTAO[0], PORTAO[1], PORTAO[2])
    oa.peca(rgb, grade, (gx0, gy0, gx1, gy1), round((gy1 - gy0) * esc), "portao_grade", tom=TOM, cores=24)
    mede("portao", c, esc, grade=(gx0, gx1, gy0, gy1), teclado=(810, 945, 600, 785), chao=(0, 1385))


def painel():
    rgb, m = oa.recorta("painel_desvios")
    c = caixa_de("painel", m)
    _, esc = por_parte("painel_desvios", rgb, m, c, PAINEL[0], PAINEL[1], PAINEL[2])
    lx0, lx1, ly0, ly1 = PAINEL_LAMPADA
    verde = rgb.copy()
    z = verde[ly0:ly1, lx0:lx1]
    vermelho = (z[:, :, 0] - z[:, :, 1]) > 40
    z[vermelho] = z[vermelho][:, [1, 0, 2]] * [1.0, 1.15, 0.9]  # troca o vermelho pelo verde
    oa.peca(verde, m, c, round((c[3] - c[1]) * esc), "painel_desvios_aceso", tom=TOM, cores=24)
    mede("painel_desvios", c, esc, lampada=(lx0, lx1, ly0, ly1))


def mezanino():
    rgb, m = oa.recorta("mezanino")
    cx0, cy0 = MEZANINO_CORTE
    m = m.copy()
    m[:, :cx0] = False
    m[:cy0] = False
    m[600:, :395] = False  # a escada embaixo do chão do mezanino, até a coluna
    c = caixa_de("mezanino", m)
    _, esc = por_parte("mezanino", rgb, m, c, MEZANINO[0], MEZANINO[1], MEZANINO[2], cores=32)
    dx0, dx1, dy = MEZANINO_DECK
    mede("mezanino", c, esc, deck0=(dx0, dy), deck1=(dx1, dy), monitor=MEZANINO_MONITOR, chao=(0, 955))


def caixotes():
    rgb, m = oa.recorta("caixotes")
    c = caixa_de("caixotes", m)
    _, esc = por_parte("caixotes", rgb, m, c, CAIXOTES[0], CAIXOTES[1], CAIXOTES[2])
    pts = {"chao": (0, CAIXOTES[1])}
    for i, (a, b, t) in enumerate(CAIXOTES_DEGRAUS):
        pts[f"degrau{i}_0"] = (a, t)
        pts[f"degrau{i}_1"] = (b, t)
    mede("caixotes", c, esc, **pts)


TETO = 110  # a parede tem 540 px e o chão fica 35 px acima da borda da tela: falta isso de teto em cima


def com_teto(rgb):
    """As vigas de cima espelhadas para cima, escurecendo até o preto (cobre a faixa da tela acima da parede)."""
    topo = rgb[:TETO][::-1].astype(float)
    k = np.linspace(0.08, 0.75, TETO)[:, None, None]  # escuro lá em cima, quase igual perto da emenda
    return np.concatenate([topo * k, rgb.astype(float)], 0)


def opaca(rgb):
    return np.dstack([rgb, np.full(rgb.shape[:2], 255)]).astype(np.uint8)


def paleta_estacao():
    e = np.array(Image.open(ESTACAO).convert("RGB")).astype(float)
    return e, np.unique(e.reshape(-1, 3), axis=0)


def colunas(a):
    """Centro de cada coluna (a faixa vermelha na altura de 170-215 px): x das emendas."""
    z = a[175:210].astype(float)
    verm = ((z[:, :, 0] > 1.5 * z[:, :, 1] + 2) & (z[:, :, 0] - z[:, :, 1] > 9)).mean(0) > 0.7
    xs, grupos = np.where(verm)[0], []
    for x in xs:
        if grupos and x - grupos[-1][1] <= 3:
            grupos[-1][1] = x
        else:
            grupos.append([x, x])
    return [(g0 + g1) // 2 for g0, g1 in grupos if g1 - g0 > 20]


def paredes():
    e, pal = paleta_estacao()
    lum_e = np.median(e[100:370] @ oa.extrai.LUM)  # só a parede: o chão do saguão é bem maior que o da plataforma
    xs = colunas(e)
    oa.salva(opaca(com_teto(e[:, :xs[-1]])), "estacao_inicio")
    medidas["estacao_inicio"] = {"w": int(xs[-1]), "h": TELA + TETO, "colunas": [int(x) for x in xs]}
    for nome in ("parede_plataforma", "parede_saguao"):
        im = Image.open(os.path.join(oa.REF, nome + ".png")).convert("RGB")
        w = round(im.width * TELA / im.height)
        a = np.array(im.resize((w, TELA), Image.BOX)).astype(float)
        cs = colunas(a)  # antes de escurecer e trocar as cores
        a *= lum_e / max(1.0, np.median(a[100:370] @ oa.extrai.LUM))
        a = pal[oa.extrai.indexa(a, pal)]
        x0, x1 = cs[0], cs[-1]
        oa.salva(opaca(com_teto(a[:, x0:x1])), nome)
        medidas[nome] = {"w": int(x1 - x0), "h": TELA + TETO, "colunas_originais": [int(x) for x in cs]}


def previa():
    """As peças em pé no chão da parede da plataforma, com o Kai, em escala 2."""
    fundo = Image.open(os.path.join(oa.OUT, "estacao_inicio.png")).convert("RGBA")
    sag = Image.open(os.path.join(oa.OUT, "parede_saguao.png")).convert("RGBA")
    plat = Image.open(os.path.join(oa.OUT, "parede_plataforma.png")).convert("RGBA")
    W = fundo.width + plat.width + sag.width * 2
    tela = Image.new("RGBA", (W, TELA), (0, 0, 0, 255))
    x = 0
    for p in (fundo, plat, sag, sag):
        tela.alpha_composite(p.crop((0, TETO, p.width, p.height)), (x, 0))
        x += p.width
    chao = CHAO_PAREDE
    kai = Image.open(oa.KAI).convert("RGBA")
    pecas = ["bilhete", "quadro_partidas", "cartaz_clinica", "achados_perdidos", "moeda", "portao_batente", "bilheteria",
             "maquina_automatica", "caixotes", "mezanino", "painel_desvios", "porta_adm"]
    x = 180
    tela.alpha_composite(kai, (60, chao - kai.height))
    for n in pecas:
        im = Image.open(os.path.join(oa.OUT, n + ".png")).convert("RGBA")
        y = chao - im.height
        if n == "quadro_partidas":
            y = 40
        elif n in ("cartaz_clinica", "painel_desvios"):
            y = chao - 150 - im.height // 2
        if n == "portao_batente":
            g = Image.open(os.path.join(oa.OUT, "portao_grade.png")).convert("RGBA")
            gm = medidas["portao"]["grade"]
            tela.alpha_composite(g, (x + round(gm[0] - gm[2] / 2), chao - round(gm[1] + gm[3] / 2)))
        tela.alpha_composite(im, (x, y))
        if n == "porta_adm":  # as folhas na frente do vão escuro
            for f in ("porta_adm_folha_esq", "porta_adm_folha_dir"):
                fm = medidas["porta_adm"][f[10:]]
                tela.alpha_composite(Image.open(os.path.join(oa.OUT, f + ".png")).convert("RGBA"),
                                     (x + round(fm[0] - fm[2] / 2), chao - round(fm[1] + fm[3] / 2)))
        x += im.width + 30
        if n in ("bilheteria", "caixotes"):
            tela.alpha_composite(kai, (x - 20, chao - kai.height))
            x += kai.width
    tela.resize((W * 2 // 3, TELA * 2 // 3), Image.NEAREST).save(os.path.join(AQUI, "previa_objetos.png"))
    tela.crop((0, 0, 1500, TELA)).save(os.path.join(AQUI, "previa_objetos_a.png"))
    tela.crop((1400, 0, 2900, TELA)).save(os.path.join(AQUI, "previa_objetos_b.png"))


def main():
    os.makedirs(oa.OUT, exist_ok=True)
    for n in ALTURA:
        if n == "quadro_partidas":
            rgb, m = verde_no_preto(n)
            c = caixa_de(n, m)
            esc = ALTURA[n] / (c[3] - c[1])
            oa.peca(rgb, m, c, ALTURA[n], n, tom=0.8, cores=24)
            mede(n, c, esc)
            continue
        rgb, m, c, esc = simples(n)
        pts = {}
        if n == "maquina_automatica":
            pts = {"bandeja": MAQUINA_BANDEJA, "moeda": MAQUINA_MOEDA, "chao": (0, c[3])}
        elif n == "bilheteria":
            pts = {"papel": BILHETERIA_PAPEL, "chao": (0, c[3])}
        mede(n, c, esc, **pts)
    porta()
    portao()
    painel()
    mezanino()
    caixotes()
    paredes()
    with open(os.path.join(oa.OUT, "medidas.json"), "w", encoding="utf-8") as f:
        json.dump(medidas, f, indent=1, ensure_ascii=False)
    previa()
    for k, v in medidas.items():
        print(k, v)


if __name__ == "__main__":
    main()
