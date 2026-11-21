"""Peças dos finais do Cenário 7 (pixel art a 58 px por unidade, no mesmo tom dos painéis).

Arte (Assets/Art/Cenarios4a7):
- c7_vala_escura.png   o fundo da vala dos trilhos some no escuro: quem cai da plataforma desaparece lá embaixo
- c7_trem_frente.png   a frente do metrô, vista de frente, saindo do túnel (final em que o Kai se joga nos trilhos)
- c7_sangue_poca.png   poça grande de sangue (morte do Souta), cresce embaixo dele
- c7_sangue_respingo.png respingos no chão, para trás de quem leva o tiro
Personagem (Assets/Art/Personagens/PixelArt/Souta):
- deitado_00.png       o Souta estendido de costas no chão, esticado (não encolhido)
Jornal dos finais (Assets/Art/Cenario7):
- jornal_papel.png, jornal_foto_eles.png, jornal_foto_mim.png
Uso: python Ferramentas/Cenario7/final_c7.py"""
import os

import numpy as np
from PIL import Image, ImageFilter

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ART = os.path.join(RAIZ, "Assets", "Art", "Cenarios4a7")
ART7 = os.path.join(RAIZ, "Assets", "Art", "Cenario7")
SOUTA = os.path.join(RAIZ, "Assets", "Art", "Personagens", "PixelArt", "Souta")
PAINEL = os.path.join(ART, "c7_painel_b.png")

BEIRA = 575          # px do painel: na linha dos pés, a quina da plataforma (o topo é em perspectiva)
VALA_Y0, VALA_Y1 = 552, 612   # o escuro começa a cobrir em VALA_Y0 e é total a partir de VALA_Y1


def vala():
    pw, ph = Image.open(PAINEL).size
    w, h = pw - BEIRA, ph - VALA_Y0 + 40     # passa um pouco da borda de baixo do painel
    s = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        a = np.clip((y) / (VALA_Y1 - VALA_Y0), 0, 1) ** 1.6
        s[y, :, :3] = (3, 3, 4)
        s[y, :, 3] = int(255 * a)
    # junto da plataforma o escuro sobe um pouco (a parede da vala)
    for x in range(min(10, w)):
        s[:, x, 3] = np.maximum(s[:, x, 3], (255 * np.clip((np.arange(h) - 4) / 30, 0, 1)).astype(np.uint8))
    return s


def painel_c6():
    """Os três painéis do Cenário 6 lado a lado: é de lá que vem o trem do cenário (cores e sujeira)."""
    ims = [Image.open(os.path.join(ART, n + ".png")).convert("RGBA") for n in ("c6_inicio", "c6_meio", "c6_fim")]
    bg = Image.new("RGBA", (sum(i.width for i in ims), ims[0].height))
    x = 0
    for i in ims:
        bg.paste(i, (x, 0))
        x += i.width
    return np.array(bg)


def trem_frente():
    """A frente do mesmo trem do Cenário 6 (o vagão pintado parado no fim do túnel), vista de frente: a lata bege
    encardida com escorridos, a faixa vermelho-escura, as janelas escuras de canto redondo. As texturas são recortadas
    do próprio vagão pintado, então a cor e a sujeira são as mesmas."""
    c6 = painel_c6()
    lata = c6[270:400, 3315:3355, :3]          # parede lisa do vagão, com os escorridos
    faixa = c6[407:432, 3315:3355, :3]         # a faixa vermelha
    saia = c6[436:470, 3315:3355, :3]          # embaixo da faixa
    teto = c6[238:262, 3330:3370, :3]          # o teto arredondado
    vidro = c6[282:372, 3524:3558, :3]         # o vidro escuro de uma janela
    moldura = np.median(c6[276:280, 3522:3560, :3].reshape(-1, 3), axis=0)
    baixo = c6[478:520, 3300:3480, :3]         # chassi escuro

    w, h = 184, 204
    rng = np.random.default_rng(6)
    s = np.zeros((h, w, 4), np.uint8)

    def ladrilha(patch, y0, y1):
        ph, pw = patch.shape[:2]
        x = 0
        while x < w:
            off = int(rng.integers(0, max(1, pw - 8)))
            larg = min(pw - off, w - x)
            alt = y1 - y0
            fonte = patch[np.arange(alt) * ph // alt][:, off:off + larg]
            s[y0:y1, x:x + larg, :3] = fonte
            x += larg
        s[y0:y1, :, 3] = 255

    ladrilha(teto, 0, 22)
    ladrilha(lata, 22, 128)
    ladrilha(faixa, 128, 146)
    ladrilha(saia, 146, 178)
    # chassi/para-choque
    bx = np.arange(w) * baixo.shape[1] // w
    s[178:h, :, :3] = baixo[np.arange(h - 178) * baixo.shape[0] // (h - 178)][:, bx]
    s[178:h, :, 3] = 255

    # cantos de cima arredondados, como a ponta do vagão
    yy, xx = np.mgrid[0:h, 0:w]
    r = 24
    for cx in (r, w - 1 - r):
        lado = (xx < r) if cx == r else (xx > w - 1 - r)
        s[lado & (yy < r) & ((xx - cx) ** 2 + (yy - r) ** 2 > r * r), 3] = 0
    m = s[..., 3] > 0
    # sombreado das laterais (a frente é curva)
    sombra = (np.abs(xx - w / 2) / (w / 2)) ** 3 * 0.35
    for c in range(3):
        s[..., c] = np.where(m, (s[..., c] * (1 - sombra)).astype(np.uint8), 0)
    borda = m & ~(np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1))
    s[borda] = (18, 16, 14, 255)

    def janela(x0, y0, x1, y1, raio=6):
        jy, jx = np.mgrid[y0:y1, x0:x1]
        dentro = np.ones(jy.shape, bool)
        for cx, cy in ((x0 + raio, y0 + raio), (x1 - 1 - raio, y0 + raio), (x0 + raio, y1 - 1 - raio), (x1 - 1 - raio, y1 - 1 - raio)):
            canto = (np.abs(jx - cx) <= raio) & (np.abs(jy - cy) <= raio) & ((jx < x0 + raio) | (jx > x1 - 1 - raio)) & (
                (jy < y0 + raio) | (jy > y1 - 1 - raio))
            dentro &= ~(canto & ((jx - cx) ** 2 + (jy - cy) ** 2 > raio * raio))
        vy = (np.arange(y1 - y0) * vidro.shape[0] // (y1 - y0))
        vx = (np.arange(x1 - x0) * vidro.shape[1] // (x1 - x0))
        miolo = vidro[vy][:, vx]
        reg = s[y0:y1, x0:x1]
        reg[dentro, :3] = miolo[dentro]
        anel = dentro & ~(np.roll(dentro, 2, 0) & np.roll(dentro, -2, 0) & np.roll(dentro, 2, 1) & np.roll(dentro, -2, 1))
        reg[anel, :3] = moldura

    janela(16, 32, 90, 104)                    # para-brisa em duas folhas, como as janelas do vagão
    janela(94, 32, 168, 104)
    janela(70, 108, 114, 176, 4)               # a porta de emergência da frente
    # letreiro apagado em cima
    s[10:22, 60:124, :3] = (16, 14, 12)
    for x in range(64, 120, 4):
        if rng.random() < 0.6:
            s[14:17, x:x + 2, :3] = (120, 66, 26)
    # faróis: as lanterninhas do vagão, acesas
    for cx in (34, w - 35):
        d = (xx - cx) ** 2 + (yy - 160) ** 2
        s[d <= 64, :3] = (40, 36, 30)
        s[d <= 30, :3] = (236, 222, 180)
        s[d <= 8, :3] = (255, 248, 225)
    # escorridos de ferrugem extra embaixo da faixa
    for _ in range(14):
        x = int(rng.integers(4, w - 4)); y0 = 146; comp = int(rng.integers(6, 26))
        s[y0:y0 + comp, x, :3] = (s[y0:y0 + comp, x, :3] * 0.7).astype(np.uint8)
    return s


def poca(w, h, semente):
    rng = np.random.default_rng(semente)
    yy, xx = np.mgrid[0:h, 0:w]
    m = np.zeros((h, w), bool)
    for _ in range(9):
        cx, rx = rng.uniform(0.2, 0.8), rng.uniform(0.12, 0.3)
        ry = rng.uniform(0.3, 0.5)
        m |= ((xx - w * cx) / (w * rx)) ** 2 + ((yy - h * 0.55) / (h * ry)) ** 2 < 1
    borda = m & ~(np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1))
    s = np.zeros((h, w, 4), np.uint8)
    s[m] = (70, 8, 8, 245)
    d = 1 - (((xx - w * 0.5) / (w * 0.4)) ** 2 + ((yy - h * 0.55) / (h * 0.45)) ** 2)
    s[m & (d > 0.35)] = (52, 5, 5, 250)
    s[borda] = (98, 16, 14, 235)
    for _ in range(int(w / 6)):
        x, y = int(rng.uniform(0.15, 0.85) * w), int(rng.uniform(0.25, 0.5) * h)
        if m[y, x]:
            s[y, x] = (130, 30, 26, 240)
    return s


def respingo():
    w, h = 140, 30
    rng = np.random.default_rng(9)
    s = np.zeros((h, w, 4), np.uint8)
    for _ in range(70):
        x = int(abs(rng.normal(0, 0.35)) * w)
        if x >= w:
            continue
        y = int(rng.uniform(0.3, 1.0) * (h - 3))
        r = 1 if x > w * 0.3 else int(rng.integers(1, 3))
        c = (90, 10, 10, 240) if rng.random() < 0.7 else (120, 24, 20, 240)
        s[max(0, y - r + 1):y + 1, max(0, w - 1 - x - r):w - x] = c   # mais denso perto do corpo (direita)
    return s


def souta_deitado():
    """O quadro parado do Souta virado de costas no chão, esticado (cabeça para a esquerda)."""
    im = Image.open(os.path.join(SOUTA, "idle_00.png")).convert("RGBA")
    corpo = im.crop(im.getbbox())
    deitado = corpo.rotate(90, expand=True, resample=Image.NEAREST)   # cabeça para a esquerda, rosto para cima
    canvas = Image.new("RGBA", im.size)
    x = (im.width - deitado.width) // 2
    canvas.alpha_composite(deitado, (x, im.height - deitado.height))
    return np.array(canvas)


def meio_tom(img, w, h, claro=0):
    """Foto de jornal: cinza, granulada, com retícula de pontos."""
    g = img.convert("L").resize((w, h), Image.LANCZOS)
    a = np.array(g).astype(float)
    a = (a - a.min()) / max(1, a.max() - a.min())
    a = np.clip(a ** 0.5 * 1.05 + 0.12 + claro, 0, 1)   # o painel é escuro: clareia os meios-tons
    yy, xx = np.mgrid[0:h, 0:w]
    passo = 4
    cx = (xx % passo) - passo / 2 + 0.5
    cy = (yy % passo) - passo / 2 + 0.5
    raio = np.sqrt(cx ** 2 + cy ** 2) / (passo * 0.72)
    tinta = raio > a                      # ponto de tinta onde a imagem é escura
    papel = np.array([226, 219, 200])
    preto = np.array([34, 32, 30])
    out = np.where(tinta[..., None], preto, papel)
    return Image.fromarray(out.astype(np.uint8), "RGB")


def jornal():
    painel = Image.open(PAINEL).convert("RGB")
    plataforma = painel.crop((0, 140, 700, 600))
    trilhos = painel.crop((600, 140, 1251, 650))
    meio_tom(plataforma, 560, 368).save(os.path.join(ART7, "jornal_foto_eles.png"))
    meio_tom(trilhos, 560, 368, 0.05).save(os.path.join(ART7, "jornal_foto_mim.png"))
    # papel: amarelado, com manchas e uma dobra no meio
    w, h = 600, 520
    rng = np.random.default_rng(11)
    base = np.ones((h, w, 3)) * np.array([233, 226, 208])
    ruido = rng.normal(0, 5, (h, w))[..., None]
    manchas = np.array(Image.fromarray((rng.random((h // 20, w // 20)) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)) / 255.0
    yy, xx = np.mgrid[0:h, 0:w]
    vinheta = (((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2) * 18
    base = base + ruido - manchas[..., None] * np.array([10, 12, 20]) - vinheta[..., None]
    dobra = np.exp(-((yy - h * 0.5) / 2.0) ** 2) * 14
    base -= dobra[..., None]
    Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB").filter(ImageFilter.GaussianBlur(0.6)).save(
        os.path.join(ART7, "jornal_papel.png"))


def main():
    for nome, a in (("c7_vala_escura", vala()), ("c7_trem_frente", trem_frente()), ("c7_sangue_poca", poca(230, 34, 4)),
                    ("c7_sangue_respingo", respingo())):
        Image.fromarray(a, "RGBA").save(os.path.join(ART, nome + ".png"))
        print(nome, a.shape[1], "x", a.shape[0])
    Image.fromarray(souta_deitado(), "RGBA").save(os.path.join(SOUTA, "deitado_00.png"))
    jornal()
    print("jornal ok")


if __name__ == "__main__":
    main()
