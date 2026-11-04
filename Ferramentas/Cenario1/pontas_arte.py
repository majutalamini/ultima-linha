"""Pontas do vagão (Cenários 0 e 1) montadas com os pixels do próprio vagão. python pontas_arte.py (depois de objetos_arte.py)

A antepara gerada não casa com o vagão: teto, faixas e chão mudam na emenda. Aqui a parte de dentro da ponta é recortada
do vagao_antigo, nas MESMAS linhas dele (teto, letreiro, parede, chão continuam retos): é só parede. Da antepara gerada
fica o lado de fora (pilar, plataforma e engate), sem a sanfona rasgada: ali é onde a porta da ponta aparece aberta.

A porta fica na linha da ponta, logo depois do pilar. Fechada, a Unity a mostra de perfil (uma lâmina); aberta, ela gira
para fora e fica de frente, no fundo, atrás do corrimão da plataforma. A folha é uma folha da porta lateral do vagão.

- ponta_dir.png: fim do vagão (Abertura). Da esquerda: parede | pilar | plataforma (a porta aberta fica atrás dela).
- ponta_esq.png: começo do vagão (Cenário 1), o mesmo desenho espelhado, com pedaços não espelhados do vagão; o biombo
  do 1º banco continua na frente. O arquivo é gravado espelhado (a Unity o põe numa raiz espelhada).
- porta_ponta_dir.png / porta_ponta_esq.png: as folhas (a esquerda também gravada espelhada).
- previa_pontas.png (aqui mesmo): as duas pontas montadas como a Unity monta (porta da direita aberta, da esquerda
  fechada), em 1:1 e ampliadas.
As medidas vão para Objetos/medidas_pontas.json e estão copiadas no Cenario1SceneBuilder.
"""
import json
import os

import numpy as np
from PIL import Image

import objetos_arte  # noqa: E402 (recorta do verde e põe na paleta do vagão)

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
OBJ = os.path.join(RAIZ, "Assets", "Art", "Cenario1", "Objetos")
VAGAO = os.path.join(RAIZ, "Assets", "Art", "Cenario0", "vagao_antigo.png")
KAI = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt", "Kai", "idle_00.png")

H = 540
PES = 505                      # linha dos pés no vagao_antigo (FloorPy)
TETO = (60, 145)               # faixa do teto (corrimão, sem luminária) ...
LETREIRO = (145, 182)          # ... faixa do letreiro em cima das portas ...
PORTA = (182, 443)             # ... altura das portas (a folha da ponta usa as mesmas linhas) ...
CHAO = 443                     # ... e o chão encerado daqui para baixo
# colunas do vagao_antigo usadas (x0, x1)
TETO_LISO = (1945, 1995)       # teto sem luminária nem alça
LETREIRO_LISO = (1983, 2003)   # faixa do letreiro depois da plaquinha (mesmo tom da emenda)
CHAPA = (1835, 1905, 395, 440) # metal da folha da porta lateral sem janela nem pichação (x0, x1, y0, y1): a parede
PORTA_QUADROS = 6              # porta da ponta abrindo: referencias/objetos/porta_ponta_animacao.png, de perfil até de frente
CHAO_LIMPO = 1823              # chão sem papel: um trecho inteiro a partir daqui (ladrilhar faz xadrez)
POSTE = (2006, 2010)           # poste logo depois da última porta lateral (começo da ponta direita)
FOLHA_ESQ = (1829, 1910)       # folhas da porta lateral: a da ponta direita ...
FOLHA_DIR = (1918, 1999)       # ... e a da esquerda
PAREDE_W = 50                  # parede da ponta: cobre o resto do vagão depois da porta lateral (e a janela cortada)
BIOMBO = (0, 104, 365, 512)    # biombo do 1º banco (x0, x1, y0, y1), na frente da parede da ponta esquerda
# onde cada ponta encosta no vagão (px do vagao_antigo)
ENCOSTA_DIR = 2006
ENCOSTA_ESQ = 45
# antepara gerada (px da antepara.png de objetos_arte.py): o pilar começa em ANT_PILAR e termina em ANT_PILAR_FIM; a
# sanfona rasgada e os corrimãos da plataforma saem (EM_CIMA, x0, x1, y0, y1): fica o pilar e a grade da plataforma
ANT_PILAR, ANT_PILAR_FIM, ANT_CHAO = 159, 183, 429
EM_CIMA = [(184, 302, 0, 392), (234, 252, 392, 400), (278, 302, 392, 418)]  # tudo acima da grade + pés dos corrimãos
MISTURA = 5                    # colunas de mistura onde a ponta encosta na arte do vagão


def faixa(v, cols, rows, largura, semente=0):
    """Tira colunas de v[rows, cols] lado a lado até a largura, começando em pontos diferentes."""
    x0, x1 = cols
    y0, y1 = rows
    rng = np.random.default_rng(semente)
    out = np.zeros((y1 - y0, largura, v.shape[2]), v.dtype)
    x = 0
    while x < largura:
        a = int(rng.integers(x0, x1 - 4))
        n = min(largura - x, x1 - a)
        out[:, x:x + n] = v[y0:y1, a:a + n]
        x += n
    return out


def parede(v):
    """Parte de dentro da ponta direita: o poste e a chapa (o metal das portas do vagão, em blocos tirados de pontos
    diferentes: o vagão não tem um trecho largo de parede lisa), com o teto, o letreiro e o chão do vagão."""
    pw = POSTE[1] - POSTE[0]
    x0, x1, y0, y1 = CHAPA
    rng = np.random.default_rng(5)
    w = PAREDE_W - pw
    chapa = np.zeros((H, w, 3), np.uint8)
    for y in range(0, H, y1 - y0):
        for x in range(0, w, 23):
            ax = int(rng.integers(x0, x1 - 23))
            n, m = min(23, w - x), min(y1 - y0, H - y)
            chapa[y:y + m, x:x + n] = v[y0:y0 + m, ax:ax + n]
    chapa[:, w // 2] = (chapa[:, w // 2] * 0.6).astype(np.uint8)  # junta entre as duas chapas
    out = np.zeros((H, PAREDE_W, 4), np.uint8)
    out[:, :, :3] = np.concatenate([v[:, POSTE[0]:POSTE[1]], chapa], 1)
    out[:, :, 3] = 255
    out[:TETO[0], :, :3] = v[:TETO[0], ENCOSTA_DIR:ENCOSTA_DIR + PAREDE_W]  # acima do teto: o preto do vagão, não chapa
    out[TETO[0]:TETO[1], :, :3] = faixa(v, TETO_LISO, TETO, PAREDE_W, 1)
    out[LETREIRO[0]:LETREIRO[1], :, :3] = faixa(v, LETREIRO_LISO, LETREIRO, PAREDE_W, 2)
    out[CHAO:, :, :3] = v[CHAO:, CHAO_LIMPO:CHAO_LIMPO + PAREDE_W]
    return out


def lado_de_fora(ant):
    """Pilar e plataforma da antepara, nas linhas do vagão (a grade pisa na linha dos pés), sem a sanfona."""
    ant = ant.copy()
    for x0, x1, y0, y1 in EM_CIMA:
        ant[y0:y1, x0:x1, 3] = 0
    fora = ant[:, ANT_PILAR:]
    out = np.zeros((H, fora.shape[1], 4), np.uint8)
    dy = PES - ANT_CHAO
    y0, y1 = max(0, dy), min(H, dy + fora.shape[0])
    out[y0:y1] = fora[y0 - dy:y1 - dy]
    return out


def mistura(ponta, v, x_vagao, colunas, lado):
    """As colunas da ponta que encostam no vagão vão do pixel do vagão para o da ponta."""
    for i in range(colunas):
        k = (i + 1) / (colunas + 1)
        xp = i if lado == "esq" else ponta.shape[1] - 1 - i
        xv = x_vagao + i if lado == "esq" else x_vagao - 1 - i
        if not (0 <= xv < v.shape[1]):
            continue
        cheio = ponta[:, xp, 3] > 0
        ponta[cheio, xp, :3] = np.round(v[cheio, xv] * (1 - k) + ponta[cheio, xp, :3] * k).astype(np.uint8)


def quadros_da_porta():
    """Os 6 quadros da porta abrindo, todos do mesmo tamanho, com a dobradiça na borda esquerda e a base na mesma linha
    (a Unity troca o quadro sem mexer na posição). Na luz e na paleta do vagão; a luz vermelha fica."""
    rgb, m = objetos_arte.recorta("porta_ponta_animacao")
    caixas = objetos_arte.pecas(m, PORTA_QUADROS)
    topo, base = min(c[1] for c in caixas), max(c[3] for c in caixas)
    larg = max(c[2] - c[0] for c in caixas)
    esc = (PORTA[1] - PORTA[0]) / (base - topo)
    out = []
    for x0, _, _, _ in caixas:
        q = objetos_arte.reduz(rgb[topo:base, x0:x0 + larg], m[topo:base, x0:x0 + larg], esc, cores=24, tom=1.0)
        out.append(objetos_arte.no_vagao(q, brilho=1.6))
    return out


def main():
    v = np.array(Image.open(VAGAO).convert("RGB"))
    ant = np.array(Image.open(os.path.join(OBJ, "antepara.png")).convert("RGBA"))
    fora = lado_de_fora(ant)

    # ---- ponta direita: parede | pilar | plataforma
    dir_ = np.concatenate([parede(v), fora], 1)
    mistura(dir_, v, ENCOSTA_DIR, MISTURA, "esq")

    # ---- ponta esquerda: pilar e plataforma espelhados | parede (o poste fica do lado de fora da chapa, perto do pilar)
    esq = np.concatenate([fora[:, ::-1], parede(v)], 1)
    W = esq.shape[1]
    # biombo do 1º banco por cima, onde a ponta cobre o vagão; a borda cortada pela arte é fechada com a borda direita
    # dele espelhada
    bx0, bx1, by0, by1 = BIOMBO
    x_img = W - ENCOSTA_ESQ
    esq[by0:by1, x_img:W, :3] = v[by0:by1, 0:ENCOSTA_ESQ]
    esq[by0:by1, x_img:W, 3] = 255
    borda = v[by0:by1, bx1 - 14:bx1][:, ::-1]
    tem = borda.max(2) > 18
    alvo = esq[by0:by1, x_img - 14:x_img]
    alvo[:, :, :3] = np.where(tem[:, :, None], borda, alvo[:, :, :3])
    alvo[:, :, 3] = np.where(tem, 255, alvo[:, :, 3])
    mistura(esq, v, ENCOSTA_ESQ, MISTURA, "dir")

    Image.fromarray(dir_, "RGBA").save(os.path.join(OBJ, "ponta_dir.png"))
    Image.fromarray(esq[:, ::-1], "RGBA").save(os.path.join(OBJ, "ponta_esq.png"))
    quadros = quadros_da_porta()
    for i, q in enumerate(quadros):
        Image.fromarray(q, "RGBA").save(os.path.join(OBJ, f"porta_ponta_{i}.png"))  # a esquerda espelha na Unity (raiz espelhada)
    for velho in ("porta_ponta_dir", "porta_ponta_esq"):
        for ext in (".png", ".png.meta"):
            if os.path.exists(os.path.join(OBJ, velho + ext)):
                os.remove(os.path.join(OBJ, velho + ext))
    folha_dir, folha_esq = quadros[-1], quadros[0][:, ::-1]

    pilar_fim = PAREDE_W + (ANT_PILAR_FIM - ANT_PILAR)
    med = {
        "w": W, "h": H, "pes": PES,
        # a dobradiça é o fim do pilar; aberta, a folha vai daqui para fora (medidas a partir do lado do vagão)
        "porta": [pilar_fim, PORTA[0], pilar_fim + folha_dir.shape[1], PORTA[1]], "quadros": len(quadros),
        "pilar_fim": pilar_fim,
        "encosta_dir": ENCOSTA_DIR, "encosta_esq": ENCOSTA_ESQ,
    }
    with open(os.path.join(OBJ, "medidas_pontas.json"), "w") as f:
        json.dump(med, f, indent=1)
    print(json.dumps(med))
    previa(v, dir_, esq, folha_dir, folha_esq, med)


def previa(v, dir_, esq, folha_dir, folha_esq, med):
    """Fim do vagão do C0 | ponta (porta aberta) | vão | ponta (porta fechada, de perfil) | começo do vagão do C1."""
    W, vao = med["w"], 255
    tela = Image.new("RGBA", (500 + W + vao + W + 500, H), (0, 0, 0, 255))
    tun = Image.open(os.path.join(OBJ, "tunel.png"))
    tela.alpha_composite(tun, ((tela.width - tun.width) // 2, H + 150 - tun.height))
    vag = Image.fromarray(v).convert("RGBA")
    tela.alpha_composite(vag.crop((ENCOSTA_DIR - 500, 0, ENCOSTA_DIR, H)), (0, 0))
    x_c1 = 500 + 2 * W + vao
    tela.alpha_composite(vag.crop((ENCOSTA_ESQ, 0, ENCOSTA_ESQ + 500, H)), (x_c1, 0))
    p = med["porta"]
    # direita aberta: a folha de frente, atrás da plataforma
    tela.alpha_composite(Image.fromarray(folha_dir, "RGBA"), (500 + p[0], p[1]))
    # esquerda fechada: o primeiro quadro (de perfil), espelhado, encostado no pilar
    tela.alpha_composite(Image.fromarray(folha_esq, "RGBA"), (x_c1 - p[0] - folha_esq.shape[1], p[1]))
    tela.alpha_composite(Image.fromarray(dir_, "RGBA"), (500, 0))
    tela.alpha_composite(Image.fromarray(esq, "RGBA"), (x_c1 - W, 0))
    kai = Image.open(KAI)
    pe = np.nonzero(np.array(kai)[:, :, 3])[0].max() + 1
    tela.alpha_composite(kai, (500 + med["pilar_fim"] + 25 - 128, PES - pe))
    tela.alpha_composite(kai, (x_c1 + 60 - 128, PES - pe))
    rgb = tela.convert("RGB")
    k = 3
    zoom = [rgb.crop((440, 40, 500 + W + 60, 540)), rgb.crop((x_c1 - W - 60, 40, x_c1 + 100, 540))]
    out = Image.new("RGB", (max(rgb.width, sum(z.width * k for z in zoom)), H + max(z.height for z in zoom) * k), (20, 20, 20))
    out.paste(rgb, (0, 0))
    x = 0
    for z in zoom:
        out.paste(z.resize((z.width * k, z.height * k), Image.NEAREST), (x, H))
        x += z.width * k
    out.save(os.path.join(AQUI, "previa_pontas.png"))


if __name__ == "__main__":
    main()
