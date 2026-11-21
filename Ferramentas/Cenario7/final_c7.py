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

BEIRA = 668          # px do painel onde a plataforma termina
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


def trem_frente():
    w, h = 184, 204
    s = np.zeros((h, w, 4), np.uint8)
    yy, xx = np.mgrid[0:h, 0:w]
    r = 26
    dentro = np.ones((h, w), bool)
    for cx, cy in ((r, r), (w - 1 - r, r)):
        canto = ((xx - cx) ** 2 + (yy - cy) ** 2 > r * r) & (np.abs(xx - cx) <= r) & (yy < r) & (
            (xx < r) if cx == r else (xx > w - 1 - r))
        dentro &= ~canto
    corpo = np.array([150, 150, 144])
    s[dentro] = (*corpo, 255)
    # sombreado: mais escuro nas laterais e embaixo
    sombra = (np.abs(xx - w / 2) / (w / 2)) ** 3 * 50 + (yy / h) ** 2 * 30
    for c in range(3):
        s[..., c] = np.where(dentro, np.clip(s[..., c] - sombra, 0, 255), 0)
    borda = dentro & ~(np.roll(dentro, 1, 0) & np.roll(dentro, -1, 0) & np.roll(dentro, 1, 1) & np.roll(dentro, -1, 1))
    s[borda] = (34, 34, 36, 255)

    def ret(x0, y0, x1, y1, c):
        s[y0:y1, x0:x1] = (*c, 255)
    # letreiro de destino com os pontos laranja
    ret(52, 12, 132, 26, (14, 12, 10))
    for x in range(56, 129, 3):
        for y in (16, 20):
            if (x * 7 + y) % 5:
                s[y:y + 2, x:x + 2] = (230, 130, 40, 255)
    # para-brisa com reflexo
    ret(16, 34, 168, 104, (18, 22, 26))
    for k in range(-60, 160, 26):
        for i in range(70):
            x, y = 16 + k + i, 34 + i
            if 16 <= x < 166 and y < 104:
                s[y, x:x + 6] = (52, 58, 64, 255)
    ret(90, 34, 94, 104, (40, 40, 42))
    ret(16, 104, 168, 108, (60, 60, 60))
    # porta de emergência da frente
    for x in (70, 112):
        ret(x, 110, x + 2, 176, (70, 70, 68))
    # faixa vermelha
    ret(2, 122, w - 2, 134, (132, 30, 26))
    ret(2, 122, w - 2, 124, (170, 48, 40))
    # faróis e lanternas
    for cx in (36, w - 37):
        d = (xx - cx) ** 2 + (yy - 152) ** 2
        s[d <= 110] = (60, 58, 52, 255)
        s[d <= 64] = (255, 244, 205, 255)
        s[d <= 16] = (255, 255, 240, 255)
    for cx in (18, w - 19):
        ret(cx - 5, 166, cx + 5, 172, (150, 24, 20))
    # para-choque e engate
    ret(0, 180, w, h, (38, 38, 40))
    ret(0, 180, w, 182, (70, 70, 72))
    ret(78, 186, 106, h, (24, 24, 26))
    # sujeira
    rng = np.random.default_rng(3)
    n = rng.integers(-8, 9, (h, w))
    m = s[..., 3] > 0
    for c in range(3):
        s[..., c] = np.where(m, np.clip(s[..., c].astype(int) + n, 0, 255), s[..., c])
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
