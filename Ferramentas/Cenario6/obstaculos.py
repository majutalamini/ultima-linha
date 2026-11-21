"""Peças do Cenário 6 (o túnel da corrida), no mesmo traço e escala dos painéis c6_inicio/meio/fim (58 px por unidade).

Grava em Assets/Art/Cenarios4a7:
- c6_caixa.png          caixote de madeira (1,1 u), para pular ou empilhar
- c6_cavalete.png       cavalete de obra com faixas vermelhas e brancas
- c6_entulho.png        monte de entulho de concreto
- c6_buraco.png         buraco no piso da plataforma (cair nele volta ao último ponto de controle)
- c6_alarme_lampada.png o vidro da luminária da parede, em vermelho (pisca por cima das luminárias pintadas)
- c6_trem.png           o vagão do painel c6_fim, recortado, virado para a direita e esticado: o trem que atropela o Kai
                        quando o tempo acaba
Uso: python Ferramentas/Cenario6/obstaculos.py"""
import os

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ART = os.path.join(RAIZ, "Assets", "Art", "Cenarios4a7")
PAINEIS = ["c6_inicio", "c6_meio", "c6_fim"]

BURACO_W = 238          # 4,1 u: a pé o pulo não alcança, correndo sim
LAMPADA = (1054, 290)   # uma das luminárias da parede (px no painel montado), de onde sai o vidro vermelho


def painel_montado():
    ims = [Image.open(os.path.join(ART, n + ".png")).convert("RGBA") for n in PAINEIS]
    bg = Image.new("RGBA", (sum(i.width for i in ims), ims[0].height))
    x = 0
    for i in ims:
        bg.paste(i, (x, 0))
        x += i.width
    return np.array(bg)


def ruido(s, m, amp, semente):
    rng = np.random.default_rng(semente)
    n = rng.integers(-amp, amp + 1, s.shape[:2])
    for c in range(3):
        s[..., c] = np.where(m, np.clip(s[..., c].astype(int) + n, 0, 255), s[..., c])
    return s


def caixa():
    w = h = 64
    s = np.zeros((h, w, 4), np.uint8)
    esc, mei, luz, frest = (32, 23, 15), (56, 42, 28), (80, 62, 41), (20, 14, 9)
    s[:, :] = (*mei, 255)
    # tábuas horizontais com frestas
    for y in (21, 42):
        s[y, :] = (*frest, 255)
        s[y + 1, :] = (*luz, 255)
    # moldura
    s[:5, :] = (*esc, 255); s[-5:, :] = (*esc, 255); s[:, :5] = (*esc, 255); s[:, -5:] = (*esc, 255)
    s[1, 1:-1] = (*luz, 255); s[1:-1, 1] = (*luz, 255)
    # travessa na diagonal
    for x in range(5, w - 5):
        y = h - 6 - int((x - 5) * (h - 11) / (w - 10))
        s[max(5, y - 2):min(h - 5, y + 3), x] = (*esc, 255)
        if 5 <= y - 3 < h - 5:
            s[y - 3, x] = (*luz, 255)
    # cantoneiras de metal
    for cx, cy in ((0, 0), (w - 8, 0), (0, h - 8), (w - 8, h - 8)):
        s[cy:cy + 8, cx:cx + 8] = (64, 64, 60, 255)
        s[cy + 3, cx + 3] = (110, 108, 100, 255)
    return ruido(s, s[..., 3] > 0, 6, 3)


def cavalete():
    w, h = 112, 72
    s = np.zeros((h, w, 4), np.uint8)
    metal, metal_luz = (54, 52, 48), (88, 86, 80)
    # pés em A
    for px in (16, 94):
        for y in range(26, h):
            d = (y - 26) * 10 // (h - 26)
            for xx in (px - d, px + d):
                s[y, xx - 1:xx + 2] = (*metal, 255)
                s[y, xx - 1] = (*metal_luz, 255)
    # tábua com faixas na diagonal
    y0, y1 = 12, 30
    for y in range(y0, y1):
        for x in range(w):
            faixa = ((x + (y - y0)) // 11) % 2
            s[y, x] = (148, 34, 28, 255) if faixa else (172, 166, 150, 255)
    s[y0, :] = (40, 36, 32, 255); s[y1 - 1, :] = (30, 26, 22, 255); s[y0:y1, 0] = (30, 26, 22, 255); s[y0:y1, -1] = (30, 26, 22, 255)
    s[y0 + 1, 1:-1] = np.minimum(s[y0 + 1, 1:-1].astype(int) + 30, 255)
    # sujeira escurecendo a pintura
    s = ruido(s, s[..., 3] > 0, 12, 8)
    rng = np.random.default_rng(4)
    for _ in range(60):
        x, y = rng.integers(1, w - 1), rng.integers(y0 + 1, y1 - 1)
        s[y, x, :3] = (s[y, x, :3] * 0.6).astype(np.uint8)
    # pisca-pisca âmbar em cima
    s[4:12, 52:60] = (40, 34, 26, 255)
    s[5:11, 53:59] = (210, 130, 36, 255)
    s[6, 54] = (255, 210, 120, 255)
    return s


def entulho():
    w, h = 132, 70
    s = np.zeros((h, w, 4), np.uint8)
    rng = np.random.default_rng(11)
    tons = [(70, 68, 62), (92, 88, 80), (54, 52, 47), (80, 76, 68)]
    yy, xx = np.mgrid[0:h, 0:w]
    monte = ((xx - w / 2) / (w / 2)) ** 2 + ((yy - h) / h) ** 2 < 1
    # pedaços de concreto: retângulos inclinados empilhados dentro do monte
    for i in range(130):
        cx, cy = rng.uniform(4, w - 4), rng.uniform(4, h)
        if not monte[min(h - 1, int(cy)), int(cx)]:
            continue
        rw, rh, ang = rng.uniform(5, 15), rng.uniform(4, 9), rng.uniform(-0.6, 0.6)
        c = tons[i % len(tons)]
        dx, dy = xx - cx, yy - cy
        u = dx * np.cos(ang) + dy * np.sin(ang)
        v = -dx * np.sin(ang) + dy * np.cos(ang)
        m = (abs(u) < rw / 2) & (abs(v) < rh / 2) & monte
        s[m] = (*c, 255)
        borda_cima = m & ~np.roll(m, 1, 0)
        s[borda_cima] = (min(c[0] + 30, 255), min(c[1] + 30, 255), min(c[2] + 28, 255), 255)
        borda_baixo = m & ~np.roll(m, -1, 0)
        s[borda_baixo] = (c[0] // 2, c[1] // 2, c[2] // 2, 255)
    # ferros retorcidos
    for x0, y0, x1, y1 in ((40, 38, 24, 8), (84, 32, 104, 6), (64, 42, 68, 12)):
        n = max(abs(x1 - x0), abs(y1 - y0))
        for k in range(n + 1):
            x = int(round(x0 + (x1 - x0) * k / n)); y = int(round(y0 + (y1 - y0) * k / n))
            s[y, x] = (104, 62, 40, 255)
            if x + 1 < w:
                s[y, x + 1] = (60, 36, 24, 255)
    return ruido(s, s[..., 3] > 0, 5, 12)


def buraco():
    """Buraco no piso: o concreto quebrado em volta e o escuro lá embaixo."""
    w, h = BURACO_W, 72
    s = np.zeros((h, w, 4), np.uint8)
    rng = np.random.default_rng(21)
    topo = 4 + np.cumsum(rng.integers(-1, 2, w)).clip(-3, 3)
    baixo = h - 6 + np.cumsum(rng.integers(-1, 2, w)).clip(-4, 3)
    esq = 6 + np.cumsum(rng.integers(-1, 2, h)).clip(-5, 5)
    dir_ = w - 7 + np.cumsum(rng.integers(-1, 2, h)).clip(-5, 5)
    for y in range(h):
        for x in range(w):
            if topo[x] <= y <= baixo[x] and esq[y] <= x <= dir_[y]:
                fundo = (y - topo[x]) / max(1, baixo[x] - topo[x])
                v = int(10 - 7 * fundo)
                s[y, x] = (v, max(0, v - 1), max(0, v - 2), 255)
    m = s[..., 3] > 0
    beira = m & ~(np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1))
    s[beira] = (62, 58, 52, 255)
    # a parede interna da laje logo abaixo da beira de trás (o concreto cortado)
    for x in range(w):
        for k in range(1, 6):
            y = topo[x] + k
            if y < h and m[y, x]:
                c = 46 - k * 6
                s[y, x] = (c, c - 2, c - 5, 255)
    # as faces quebradas da laje dos dois lados (cortes claros e irregulares)
    for y in range(h):
        for lado, x in ((1, esq[y]), (-1, dir_[y])):
            for k in range(3):
                xx = x + lado * k
                if 0 <= xx < w and m[y, xx]:
                    c = 70 - k * 14
                    s[y, xx] = (c, c - 3, c - 7, 255)
    # pedaços de piso soltos na beira e ferros saindo
    for _ in range(10):
        x = int(rng.uniform(10, w - 14)); y = int(topo[x]) - 1
        s[max(0, y - 2):y + 1, x:x + int(rng.integers(3, 8))] = (78, 74, 66, 255)
    for x in (34, 97, 160, 205):
        y = int(topo[x]) + 2
        for k in range(9):
            if y + k < h:
                s[y + k, x + k // 3] = (98, 58, 38, 255)
    return s


def lampada(bg):
    """O vidro da luminária, em vermelho: só os pixels claros do vidro (a grade fica de fora)."""
    cx, cy = LAMPADA
    rec = bg[cy - 30:cy + 30, cx - 18:cx + 18].astype(int)
    lum = rec[..., :3].sum(2)
    s = np.zeros(rec.shape, np.uint8)
    m = lum > 240
    k = np.clip((lum - 240) / 200.0, 0, 1)
    s[..., 0] = np.where(m, 200 + 55 * k, 0)
    s[..., 1] = np.where(m, 30 + 60 * k, 0)
    s[..., 2] = np.where(m, 24 + 40 * k, 0)
    s[..., 3] = np.where(m, 255, 0)
    return s


def trem(bg):
    """O vagão parado do painel c6_fim, recortado do túnel, virado com a frente para a direita e com mais dois trechos
    de janelas atrás, para ficar comprido o bastante para atravessar a tela."""
    x0, x1, y0, y1 = 3248, 3727, 220, 545
    r = bg[y0:y1, x0:x1].copy()
    h, w = r.shape[:2]
    frente = 3262 - x0
    for y in range(h):
        ay = y + y0
        if ay < 262:
            borda = 3302 - np.sqrt(max(0.0, 40 ** 2 - (262 - ay) ** 2)) - x0
        else:
            borda = frente
        engate = 488 <= ay <= 540
        lim = 0 if engate else int(np.ceil(borda))
        r[y, :lim, 3] = 0
        if ay < 223:
            r[y, :, 3] = 0
    trecho = r[:, 232:479].copy()          # janelas e porta do meio do vagão
    long = np.concatenate([r, trecho, trecho], axis=1)  # a frente fica na ponta esquerda
    return long[:, ::-1].copy()            # frente para a direita


def main():
    bg = painel_montado()
    saidas = {
        "c6_caixa": caixa(), "c6_cavalete": cavalete(), "c6_entulho": entulho(), "c6_buraco": buraco(),
        "c6_alarme_lampada": lampada(bg), "c6_trem": trem(bg),
    }
    for n, a in saidas.items():
        Image.fromarray(a, "RGBA").save(os.path.join(ART, n + ".png"))
        print(f"{n}.png {a.shape[1]}x{a.shape[0]}")


if __name__ == "__main__":
    main()
