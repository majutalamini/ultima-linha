"""Arte definitiva da Abertura a partir das imagens do gerador (referencias/). python abertura_arte.py

Os fundos saem na mesma grade de pixels dos personagens (58 px por unidade) e com paleta reduzida: assim cenário e
personagens têm o mesmo tamanho de pixel e não parecem duas camadas coladas. Os dois trens ainda são reduzidos por
ESCALA_CENARIO: o vagão pintado era grande demais para os personagens (porta 1,4x a altura do Kai, assento a 31% dela);
reduzido, ele cabe exatamente na altura da tela e a porta fica 1,2x o Kai. Os bancos ganham profundidade (banco_fundo).
- vagao_limpo.png: o trem limpo ampliado e encaixado sobre o decrépito (portas e bancos nas mesmas posições).
- vagao_antigo.png: o fundo do Cenário 1 (trem decrépito) na grade dos personagens.
- entre_vagoes.png: o vão entre os vagões, a partir da plataforma (a arte vale 80 px por unidade: o vão fica com 4,4 unidades).
- Passageiros/pe_N e sentado_N, Kai/acorda_NN: quadros em pixel art; os sentados já vêm com o quadril no assento
  (o quadro fica com a base no chão do vagão; as pernas pendem na frente do banco, como no desenho).
- Passageiros/sombra_sentado_N, Kai/sombra_acorda_00: a sombra que quem está sentado faz no encosto e no assento.
- previa_limpo.png / previa_antigo.png (aqui mesmo): o vagão com todo mundo no lugar, como a Unity monta. Conferir antes de abrir a Unity.
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
import sujeira  # noqa: E402

REF = os.path.join(AQUI, "referencias")
ART0 = os.path.join(RAIZ, "Assets", "Art", "Cenario0")
PIX = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt")

# trem limpo -> coordenadas do fundo decrépito (4230x1101): portas na horizontal; na vertical, o topo do encosto e o do
# assento dos dois trens coincidem (encosto ~680, assento ~806 no fundo final)
ESCALA, OX, OY = 2.12, 1, -25
LARGURA, ALTURA = 4230, 1101
# bancos, em px do fundo final (iguais nos dois trens): encosto, junta encosto/assento, quina do assento, base no chão
ENCOSTO, JUNTA, ASSENTO, BASE = 680, 798, 802, 930
ASSENTO_FUNDO = 840          # quadril mais baixo que ainda parece sentado (na frente da almofada)
PE_BANCO = 935               # chão logo na frente dos bancos, onde apoiam os pés de quem senta
CHAO = 870 + 160             # onde a Unity apoia a base dos quadros (FloorPy + 160)
BANCOS = [(205, 1045), (1460, 2370), (2785, 3695)]  # x com os braços de metal (Benches do Cenário 1)

ALTURA_KAI = 222
PPU = 58
ESCALA_CENARIO = (540.0 / PPU) / (ALTURA / 100.0)   # vagão da altura da tela (câmera 4,655 = 540/2/58)
FUNDO_POR_QUADRO = 100.0 / (PPU * ESCALA_CENARIO)  # px do fundo por px do personagem
# tom dos personagens em cada trem (o mesmo do Cenario0SceneBuilder / Cenario0Director)
TOM_LIMPO, TOM_ANTIGO = 0.92, (0.62, 0.6, 0.64)
# sombra nos pés e reflexo no chão (os mesmos AlfaSombra / AlfaReflexo do Cenario1SceneBuilder)
ALFA_SOMBRA, ALFA_REFLEXO = 0.9, 0.22
ESCALA_SOMBRA, DESCE_SOMBRA = 0.6, 0.05  # tamanho da sombra_pes e quanto o centro dela fica abaixo da sola (unidades)


def pixeliza(im, ppu_origem, cores, nome, escala=1.0):
    """Reduz para 58 px por unidade (média de cada bloco) e prende numa paleta de cores que existem na arte."""
    f = PPU * escala / ppu_origem
    p = np.array(im.convert("RGB").resize((round(im.width * f), round(im.height * f)), Image.BOX)).astype(float)
    px = p.reshape(-1, 3)
    pal = extrai.paleta(px, cores)
    # cores fortes ocupam pouca área (cartazes) e somem na paleta geral: ganham uma paleta só delas
    viva = px[px.max(1) - px.min(1) > 60]
    if len(viva) > 500:
        pal = np.vstack([pal, extrai.paleta(viva, 24)])
    Image.fromarray(pal[extrai.indexa(p, pal)].astype(np.uint8), "RGB").save(os.path.join(ART0, nome))


def trem_limpo():
    im = Image.open(os.path.join(REF, "trem_limpo.png")).convert("RGB")
    g = im.resize((round(im.width * ESCALA), round(im.height * ESCALA)), Image.LANCZOS)
    tela = Image.new("RGB", (LARGURA, ALTURA), (0, 0, 0))
    tela.paste(g, (OX, OY))
    # a imagem tem uma tarja preta em cima; reduzido, o vagão mostra até o teto: a tarja vira teto escurecendo
    a = np.array(tela).astype(float)
    y0 = int(np.argmax(a.mean((1, 2)) > 25))
    if y0 > 0:
        teto = np.median(a[y0:y0 + 6], axis=0)
        a[:y0] = teto[None] * np.linspace(0.3, 1.0, y0)[:, None, None]
        tela = Image.fromarray(a.astype(np.uint8), "RGB")
    pixeliza(banco_fundo(tela), 100, 64, "vagao_limpo.png", ESCALA_CENARIO)


def banco_fundo(im):
    """O banco pintado de frente parece parte da parede: sombra no chão embaixo dele, quina clara no assento, junta
    escura com o encosto e a parte de baixo do encosto mais escura (onde quem senta faz sombra)."""
    a = np.array(im.convert("RGB")).astype(float)
    for x0, x1 in BANCOS:
        xs = np.arange(x0 - 12, x1 + 12)
        borda = np.clip(np.minimum(xs - (x0 - 12), (x1 + 12) - xs) / 16.0, 0, 1)  # some nas pontas
        for y in range(BASE - 4, BASE + 34):                                         # chão: 45% -> 0
            k = 0.45 * min(1.0, (BASE + 34 - y) / 34.0)
            a[y, xs] *= (1 - k * borda)[:, None]
        for y in range(JUNTA - 40, JUNTA):                                            # pé do encosto
            a[y, x0:x1] *= 1 - 0.18 * (y - (JUNTA - 40)) / 40.0
        a[JUNTA - 2:ASSENTO, x0:x1] *= 0.55                                           # junta
        a[ASSENTO:ASSENTO + 3, x0:x1] += (255 - a[ASSENTO:ASSENTO + 3, x0:x1]) * 0.22  # quina do assento
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB")


def sombra_de(q):
    """Sombra de quem está sentado no encosto e no assento: a silhueta borrada, um pouco abaixo, a 35%."""
    from PIL import ImageFilter
    m = Image.fromarray(q[:, :, 3]).filter(ImageFilter.GaussianBlur(3))
    al = np.roll(np.array(m).astype(float), 5, axis=0) * 0.35
    s = np.zeros_like(q)
    s[:, :, 3] = al.astype(np.uint8)
    return Image.fromarray(s, "RGBA")


def contato():
    """Sombra de contato em pixel art (bordas duras), logo abaixo das solas: o que faz o pé parecer pisar no chão.
    PixelArt/sombra_contato.png, 58 px por unidade como os personagens; o miolo é mais escuro que a borda."""
    w, h = 56, 8
    y, x = np.mgrid[0:h, 0:w]
    d = ((x - (w - 1) / 2) / (w / 2)) ** 2 + ((y - (h - 1) / 2) / (h / 2)) ** 2
    a = np.zeros((h, w, 4), np.uint8)
    a[:, :, 3] = np.where(d < 0.35, 200, np.where(d < 1, 120, 0))
    Image.fromarray(a, "RGBA").save(os.path.join(PIX, "sombra_contato.png"))


def sem_fiapos(cheio):
    """Pedaços soltos que vieram da folha (pé de cadeira, alça de bolsa): fica só o corpo."""
    rot, cs = fatia.componentes(cheio, passo=1)
    maior = max(c[4] for c in cs)
    return cheio & np.isin(rot, [c[5] for c in cs if c[4] >= 0.1 * maior])


def perna_frente(ps):
    """Sentados de frente: altura do assento acima dos pés (px da folha), pela fenda entre as pernas, que sobe dos sapatos
    até a parte de baixo das coxas. Saia longa ou joelhos colados fecham a fenda cedo: vale a mediana das que sobem bem.
    Todos da folha sentam no mesmo banco invisível, então a medida serve para a folha inteira."""
    medidas = []
    for x0, y0, x1, y1, mp in ps:
        base = mp[y1 - max(4, (y1 - y0) // 20):y1]
        cs = np.nonzero(base.any(0))[0]
        c = (cs.min() + cs.max()) // 2
        y = y1 - 1
        while y > y0 and not mp[y, c - 2:c + 3].any():
            y -= 1
        if y1 - y > 0.25 * (y1 - y0):
            medidas.append(y1 - y)
    assert medidas, "nenhuma figura com a fenda entre as pernas"
    return float(np.median(medidas))


def faixas(m, n):
    """Figuras encostadas umas nas outras: corta nas colunas mais vazias perto das divisões regulares da folha."""
    cols = m.sum(0)
    w = m.shape[1]
    cortes = [0]
    for k in range(1, n):
        c = round(w * k / n)
        janela = cols[c - 90:c + 90]
        cortes.append(c - 90 + int(np.argmin(janela)))
    cortes.append(w)
    saida = []
    for a, b in zip(cortes, cortes[1:]):
        mp = np.zeros_like(m)
        mp[:, a:b] = m[:, a:b]
        ys, xs = np.nonzero(mp)
        saida.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1, mp))
    return saida


def figuras(folha, n, escala, destino, nomes, sentado, saturacao, ancora=lambda i: "pes", paleta=None):
    rgb = np.array(Image.open(os.path.join(REF, folha)).convert("RGB"))
    m = fatia.mascara(rgb)
    ps = fatia.poses(m, n)
    if len(ps) < n or min(p[4].sum() for p in ps) < 0.2 * max(p[4].sum() for p in ps):  # sobrou fiapo: figuras grudadas
        ps = faixas(m, n)
    # sentado(i): 1 = sentado no banco; entre 0 e 1 = levantando (pés nessa fração do caminho entre o banco e o corredor)
    perna_folha = perna_frente([p for i, p in enumerate(ps) if sentado(i) == 1]) if any(sentado(i) == 1 for i in range(n)) else 0
    fatia.SATURACAO = saturacao
    pal = fatia.neutro(extrai.paleta(rgb[m][::30].astype(float), fatia.CORES)) if paleta is None else paleta.copy()
    # o verde do fundo vaza na borda das figuras: nenhuma cor da paleta fica puxada para o verde
    outro = pal[:, [0, 2]].max(1)
    pal[:, 1] = np.minimum(pal[:, 1], outro)
    alturas = []
    for i, (x0, y0, x1, y1, mp) in enumerate(ps):
        cor, cheio = fatia.reduz(rgb[y0:y1, x0:x1], mp[y0:y1, x0:x1], escala)
        idx = extrai.indexa(cor, pal)
        idx, cheio = extrai.limpa(idx, cheio, pal, passadas=1)
        cheio = sem_fiapos(cheio)
        ys = np.nonzero(cheio.any(1))[0]
        idx, cheio = idx[ys.min():ys.max() + 1], cheio[ys.min():ys.max() + 1]
        cx = fatia.ancora(cor, cheio, ancora(i))
        h, w = cheio.shape
        sobe = 0
        if sentado(i) == 1:
            # pés no chão da frente do banco; se as pernas não alcançam (ou sobram), o quadril escorrega dentro do assento
            perna = perna_folha * escala * FUNDO_POR_QUADRO
            quad = min(max(PE_BANCO - perna, ASSENTO), ASSENTO_FUNDO)
            sobe = round((CHAO - quad - perna) / FUNDO_POR_QUADRO)
            assert sobe >= 0, (folha, i, "pernas mais compridas que a altura do assento")
        elif sentado(i):
            sobe = round(sentado(i) * (CHAO - PE_BANCO) / FUNDO_POR_QUADRO)
        q = np.zeros((fatia.QUADRO, fatia.QUADRO, 4), np.uint8)
        ox, oy = fatia.QUADRO // 2 - cx, fatia.QUADRO - h - sobe
        assert oy >= 0, (folha, i, h, sobe)
        xs0, xs1 = max(0, -ox), min(w, fatia.QUADRO - ox)
        q[oy:oy + h, ox + xs0:ox + xs1, :3] = pal[idx[:, xs0:xs1]]
        q[oy:oy + h, ox + xs0:ox + xs1, 3] = cheio[:, xs0:xs1] * 255
        os.makedirs(destino, exist_ok=True)
        Image.fromarray(q, "RGBA").save(os.path.join(destino, nomes(i) + ".png"))
        if sentado(i) == 1:
            sombra_de(q).save(os.path.join(destino, "sombra_" + nomes(i) + ".png"))
        alturas.append((h, sobe))
    return alturas


def previa():
    """Monta o vagão como a Unity, já na grade de 58 px por unidade: quadro com base em (x, CHAO) e a sombra nos pés
    (no chão para quem está em pé, em PE_BANCO para quem está sentado). Passageiros e Kai vêm do Cenario0SceneBuilder.cs."""
    import re
    from PIL import ImageDraw
    cs = open(os.path.join(RAIZ, "Assets", "Editor", "UltimaLinha", "Cenario0SceneBuilder.cs"), encoding="utf-8").read()
    gente = [(a, float(x), e == "true") for a, x, e in re.findall(r'\("((?:pe|sentado)_\d+)", (\d+)f, (true|false)\)', cs)]
    kai_x = float(re.search(r"KaiX = (\d+)f", cs).group(1))
    g = PPU * ESCALA_CENARIO / 100.0

    def cola(tela, png, x, espelha=False, tom=1.0, sentado=False):
        q = Image.open(png).convert("RGBA")
        if espelha:
            q = q.transpose(Image.FLIP_LEFT_RIGHT)
        a = np.array(q).astype(float)
        a[:, :, :3] *= tom
        q = Image.fromarray(a.astype(np.uint8), "RGBA")
        pos = (round(x * g) - q.width // 2, round(CHAO * g) - q.height)
        # reflexo no chão encerado: o quadro de cabeça para baixo a partir dos pés (a borda da tela corta o resto)
        pe = int(np.nonzero(a[:, :, 3].any(1))[0].max())
        ref = a[:pe + 1][::-1].copy()
        ref[:, :, 3] *= ALFA_REFLEXO
        ref = ref[:max(0, tela.height - (pos[1] + pe + 1))]
        if len(ref):
            tela.alpha_composite(Image.fromarray(ref.astype(np.uint8), "RGBA"), (pos[0], pos[1] + pe + 1))
        # a sombra dos pés da Unity (sombra_pes a 100 px/u, ESCALA_SOMBRA, centro DESCE_SOMBRA abaixo da sola)
        so = Image.open(os.path.join(RAIZ, "Assets", "Art", "Cenario1", "sombra_pes.png")).convert("RGBA")
        so = so.resize((round(so.width / 100 * ESCALA_SOMBRA * PPU), round(so.height / 100 * ESCALA_SOMBRA * PPU)), Image.BILINEAR)
        sa = np.array(so).astype(float)
        sa[:, :, :3] = 0
        sa[:, :, 3] *= ALFA_SOMBRA
        sy = pos[1] + pe + round(DESCE_SOMBRA * PPU)
        tela.alpha_composite(Image.fromarray(sa.astype(np.uint8), "RGBA"), (round(x * g) - so.width // 2, sy - so.height // 2))
        ct = Image.open(os.path.join(PIX, "sombra_contato.png"))
        tela.alpha_composite(ct, (round(x * g) - ct.width // 2, pos[1] + pe + 1 - ct.height // 2))
        if sentado:
            s = os.path.join(os.path.dirname(png), "sombra_" + os.path.basename(png))
            if os.path.exists(s):
                so = Image.open(s)
                tela.alpha_composite(so.transpose(Image.FLIP_LEFT_RIGHT) if espelha else so, pos)
        tela.alpha_composite(q, pos)

    pas = os.path.join(PIX, "Passageiros")
    limpo = Image.open(os.path.join(ART0, "vagao_limpo.png")).convert("RGBA")
    for a, x, e in gente:
        cola(limpo, os.path.join(pas, a + ".png"), x, e, TOM_LIMPO, a.startswith("sentado"))
    cola(limpo, os.path.join(PIX, "Kai", "acorda_00.png"), kai_x, sentado=True)
    limpo.convert("RGB").save(os.path.join(AQUI, "previa_limpo.png"))
    antigo = Image.open(os.path.join(ART0, "vagao_antigo.png")).convert("RGBA")
    for i, x in enumerate(range(int(kai_x) - 900, int(kai_x) + 900, 300)):
        cola(antigo, os.path.join(PIX, "Kai", f"acorda_{i:02d}.png"), x, tom=np.array(TOM_ANTIGO), sentado=i < 4)
    antigo.convert("RGB").save(os.path.join(AQUI, "previa_antigo.png"))


def main():
    os.makedirs(ART0, exist_ok=True)
    trem_limpo()
    contato()
    antigo = Image.open(os.path.join(RAIZ, "Assets", "Art", "Cenario1", "cenario1_fundo.png"))
    pixeliza(banco_fundo(antigo), 100, 48, "vagao_antigo.png", ESCALA_CENARIO)
    # o vão começa na plataforma (x 400 da arte): a parte de fora do vagão de trás sairia por cima do fundo do vagão
    pixeliza(Image.open(os.path.join(REF, "entre_vagoes.png")).crop((400, 0, 1536, 1024)), 80, 48, "entre_vagoes.png")

    pas = os.path.join(PIX, "Passageiros")
    for f in os.listdir(pas) if os.path.isdir(pas) else []:
        if f.startswith("p") and f[1].isdigit():
            os.remove(os.path.join(pas, f))  # provisórios
    def altura(folha, n, i):
        p = fatia.poses(fatia.mascara(np.array(Image.open(os.path.join(REF, folha)).convert("RGB"))), n)[i]
        return p[3] - p[1]

    # escala: o homem de terno (1ª figura em pé) tem a altura do Kai
    esc_pas = ALTURA_KAI / altura("passageiros_pe.png", 6, 0)
    print("em pé", figuras("passageiros_pe.png", 6, esc_pas, pas, lambda i: f"pe_{i + 1}", lambda i: False, 0.7))
    # segunda leva em pé (só braços para baixo): o rapaz de mochila tem a altura do Kai
    print("em pé 2", figuras("passageiros_pe2.png", 4, ALTURA_KAI / altura("passageiros_pe2.png", 4, 3), pas,
                             lambda i: f"pe_{i + 7}", lambda i: False, 0.7))

    # Kai acordando: sentado de frente (0-6), levantando (7), em pé de frente (8) e de perfil (9), com a altura de sempre.
    # Cores: a paleta do Kai (fatia.py), encardida como nos outros quadros; dormindo no trem limpo ele está limpo.
    with open(os.path.join(RAIZ, "Ferramentas", "PixelArt", "folhas", "kai", "paleta_limpa.json")) as f:
        limpa = np.array(json.load(f), int)
    suja = sujeira.suja(limpa)
    alt_kai = altura("kai_acorda.png", 10, 9)
    esc_kai = ALTURA_KAI / alt_kai
    kai = os.path.join(PIX, "Kai")
    print("kai", figuras("kai_acorda.png", 10, esc_kai, kai, lambda i: f"acorda_{i:02d}", lambda i: 1 if i < 7 else 0.5 if i == 7 else 0, 0.45,
                         lambda i: "pes" if i == 9 else "tronco", suja))
    a = np.array(Image.open(os.path.join(kai, "acorda_00.png")).convert("RGBA"))
    a[:, :, :3] = limpa[extrai.indexa(a[:, :, :3].astype(int), suja)]  # cada cor suja volta para a limpa de origem
    Image.fromarray(a, "RGBA").save(os.path.join(kai, "dormelimpo_00.png"))

    # sentados de frente, sem ninguém em pé na folha: o homem de terno sentado tem a altura do Kai sentado
    esc_sent = ALTURA_KAI * altura("kai_acorda.png", 10, 3) / alt_kai / altura("passageiros_sentados.png", 6, 0)
    print("sentados", figuras("passageiros_sentados.png", 6, esc_sent, pas, lambda i: f"sentado_{i + 1}", lambda i: True, 0.7,
                              lambda i: "tronco"))
    previa()


if __name__ == "__main__":
    main()
