"""
Personagens articulados de perfil (Kai, Souta, Ren) para o Cenário 1.

Cada personagem é desenhado em partes separadas (cabeça, tronco, braços, pernas, pés), cada uma com o
pivô na articulação. A Unity monta o "esqueleto" e anima por código (CharacterRig2D).
Gera:  out/Art/Cenario1/Rig/{id}_{parte}.png  +  rig_layout.json (pivôs e posições das juntas)

Coordenadas de desenho: x para a direita (para onde o personagem olha), y para cima,
origem no pivô da parte, unidades em "px base" (personagem com ~350 px de altura).
"""
import json, math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops

OUT = 'out/Art/Cenario1/Rig'
os.makedirs(OUT, exist_ok=True)
S = 4          # supersampling do desenho
EXPORT = 2     # exporta em 2x (PPU 200 na Unity)

# ---------------------------------------------------------------- geometria
def catmull(pts, n=10, closed=True):
    P = list(pts)
    if closed:
        P = [P[-1]] + P + [P[0], P[1]]
    else:
        P = [P[0]] + P + [P[-1]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = map(np.array, (P[i - 1], P[i], P[i + 1], P[i + 2]))
        for t in np.linspace(0, 1, n, endpoint=False):
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)))
    if not closed:
        out.append(tuple(P[-2]))
    return out


class Part:
    """Uma parte do corpo desenhada em camadas (formas com cor), depois sombreada."""
    def __init__(self, bounds, margin=6):
        x0, y0, x1, y1 = bounds
        self.x0, self.y0, self.x1, self.y1 = x0 - margin, y0 - margin, x1 + margin, y1 + margin
        self.w = int((self.x1 - self.x0) * S)
        self.h = int((self.y1 - self.y0) * S)
        self.img = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def P(self, pts):
        return [((x - self.x0) * S, (self.y1 - y) * S) for x, y in pts]

    def poly(self, pts, color, smooth=True):
        if smooth:
            pts = catmull(pts)
        self.d.polygon(self.P(pts), fill=color)

    def line(self, pts, color, width, smooth=False):
        if smooth:
            pts = catmull(pts, closed=False)
        self.d.line(self.P(pts), fill=color, width=max(1, int(width * S)), joint='curve')

    def ellipse(self, cx, cy, rx, ry, color):
        (a, b), (c, d) = self.P([(cx - rx, cy + ry), (cx + rx, cy - ry)])
        self.d.ellipse([a, b, c, d], fill=color)

    def finish(self, light=(186, 196, 200), rim=0.38, top_rim=True, back=False, dark=0.6, outline=0.65):
        """Clima do jogo: cores escurecidas, luz de borda vinda de trás (janelas) e de cima (teto),
        contorno escuro só por fora. Sem gradiente por parte para as juntas não aparecerem."""
        img = self.img
        a = np.asarray(img).astype(np.float32)
        alpha = a[..., 3] / 255.0
        a[..., :3] *= dark
        m = Image.fromarray((alpha * 255).astype(np.uint8))
        # borda de luz: lado de trás (esquerda) e, para tronco/cabeça, o topo
        sh1 = np.asarray(ImageChops.offset(m, int(2.4 * S), 0)).astype(np.float32) / 255
        edge = np.clip(alpha - sh1, 0, 1)
        if top_rim:
            sh2 = np.asarray(ImageChops.offset(m, int(0.6 * S), int(2.2 * S))).astype(np.float32) / 255
            edge = np.maximum(edge, np.clip(alpha - sh2, 0, 1) * 0.8)
        edge = np.asarray(Image.fromarray((np.clip(edge, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7 * S))).astype(np.float32) / 255
        edge *= alpha
        for c in range(3):
            a[..., c] = a[..., c] * (1 - edge * rim) + light[c] * edge * rim
        # lado da frente levemente na sombra
        sh3 = np.asarray(ImageChops.offset(m, -int(3.5 * S), 0)).astype(np.float32) / 255
        shade = np.asarray(Image.fromarray((np.clip(alpha - sh3, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2.5 * S))).astype(np.float32) / 255
        a[..., :3] *= (1 - shade * 0.35)[..., None]
        if back:  # membros do fundo: mais escuros
            a[..., :3] *= 0.6
        # contorno escuro suave por fora
        dil = m.filter(ImageFilter.MaxFilter(int(1.5 * S) // 2 * 2 + 1)).filter(ImageFilter.GaussianBlur(0.4 * S))
        dl = np.asarray(dil).astype(np.float32) / 255 * outline
        out = Image.new('RGBA', img.size, (8, 8, 10, 0))
        out.putalpha(Image.fromarray((dl * 255).astype(np.uint8)))
        body = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA')
        out = Image.alpha_composite(out, body)
        small = out.resize((max(1, self.w * EXPORT // S), max(1, self.h * EXPORT // S)), Image.LANCZOS)
        pivot = ((0 - self.x0) / (self.x1 - self.x0), (0 - self.y0) / (self.y1 - self.y0))
        return small, pivot


# ---------------------------------------------------------------- personagens
C = {
    'kai': dict(skin=(170, 138, 120), hair=(26, 25, 30), shirt=(124, 124, 126), shirt2=(98, 98, 102),
                tie=(34, 38, 58), pants=(28, 28, 32), shoe=(16, 15, 16), belt=(14, 13, 13)),
    'souta': dict(skin=(178, 146, 124), hair=(104, 84, 66), shirt=(150, 146, 138), shirt2=(118, 114, 108),
                  tie=(30, 32, 44), pants=(30, 30, 36), shoe=(18, 17, 18), belt=(16, 15, 14), clip=(210, 208, 200)),
    'ren': dict(skin=(174, 144, 126), hair=(150, 134, 104), shirt=(136, 132, 126), shirt2=(110, 106, 100),
                tie=(30, 30, 36), pants=(30, 29, 32), shoe=(17, 16, 17), belt=(15, 14, 13),
                cardigan=(62, 48, 40), cardigan2=(46, 36, 30), glasses=(40, 38, 36)),
}
A = lambda c, a=255: tuple(c) + (a,)
DARK = lambda c, k: tuple(int(v * k) for v in c)


def torso(cid):
    c = C[cid]
    cardigan = cid == 'ren'
    p = Part((-30, -34 if cardigan else -18, 30, 140))
    kx = 1.18
    _poly, _line = p.poly, p.line
    p.poly = lambda pts, color, smooth=True: _poly([(x * kx, y) for x, y in pts], color, smooth)
    p.line = lambda pts, color, width, smooth=False: _line([(x * kx, y) for x, y in pts], color, width, smooth)
    # calça (quadril)
    p.poly([(-17, 8), (-19, -4), (-16, -16), (0, -18), (13, -14), (15, -2), (14, 8)], A(c['pants']))
    # camisa
    p.poly([(-16, 6), (-18, 40), (-20, 80), (-20, 106), (-15, 119), (-4, 125), (7, 123), (14, 114),
            (18, 92), (17, 64), (14, 30), (13, 8)], A(c['shirt']))
    # dobras/sombra da camisa
    p.poly([(-16, 6), (-18, 40), (-19, 80), (-17, 108), (-12, 118), (-9, 100), (-11, 60), (-10, 20)], A(c['shirt2']), smooth=True)
    for y0 in (28, 52, 70):
        p.line([(-6, y0), (2, y0 - 4), (10, y0 - 2)], A(c['shirt2'], 160), 1.4, smooth=True)
    # gola
    p.poly([(2, 118), (10, 121), (15, 112), (9, 108)], A(DARK(c['shirt'], 1.08)))
    # cinto
    if not cardigan:
        p.poly([(-16, 4), (13, 4), (14, 11), (-16, 11)], A(c['belt']), smooth=False)
        p.poly([(9, 4), (14, 4), (14, 11), (9, 11)], A((120, 116, 104)), smooth=False)
    # gravata
    if cid == 'kai':  # frouxa e torta
        p.poly([(9, 114), (14, 113), (17, 104), (19, 70), (22, 48), (16, 44), (13, 70), (11, 104)], A(c['tie']))
    else:
        p.poly([(9, 116), (14, 116), (16, 106), (18, 60), (17, 40), (13, 40), (12, 60), (11, 106)], A(c['tie']))
    if cid == 'souta':
        p.poly([(9, 78), (20, 78), (20, 81), (9, 81)], A(c['clip']), smooth=False)
    if cardigan:  # cardigã largo e comprido por cima
        p.poly([(-20, -30), (-22, 20), (-22, 70), (-20, 104), (-13, 118), (-2, 121), (4, 116), (7, 96),
                (9, 70), (10, 30), (11, -6), (12, -30), (0, -34)], A(c['cardigan']))
        p.line([(6, 112), (9, 80), (10, 40), (11, -28)], A(c['cardigan2']), 2.4, smooth=True)
        for y0 in range(-20, 100, 9):
            p.line([(-18, y0), (6, y0 + 1)], A(c['cardigan2'], 90), 0.8)
    return p.finish()


def head(cid):
    """Cabeça de perfil olhando para a direita. Pivô na base do pescoço."""
    c = C[cid]
    p = Part((-30, -4, 34, 82))
    k = 1.12  # cabeça um pouco maior que o desenho base
    _poly, _line, _ell = p.poly, p.line, p.ellipse
    sc = lambda pts: [(x * k, (y - 13) * k + 11) if y > 15 else (x, y) for x, y in pts]
    p.poly = lambda pts, color, smooth=True: _poly(sc(pts), color, smooth)
    p.line = lambda pts, color, width, smooth=False: _line(sc(pts), color, width, smooth)
    p.ellipse = lambda cx, cy, rx, ry, color: _ell(cx * k, (cy - 13) * k + 11, rx * k, ry * k, color)
    # pescoço
    p.poly([(-6, -2), (-7, 12), (-4, 18), (6, 18), (8, 10), (7, -2)], A(DARK(c['skin'], 0.82)))
    # crânio + rosto (perfil)
    face = [(-15, 30), (-17, 44), (-14, 58), (-4, 66), (8, 66), (17, 58), (20, 48), (21, 42),
            (25, 37), (21, 34), (20, 29), (19, 25), (17, 22), (16, 17), (10, 13), (2, 13), (-6, 18), (-12, 22)]
    p.poly(face, A(c['skin']))
    # orelha
    p.poly([(-4, 34), (-8, 38), (-7, 46), (-2, 46), (0, 40)], A(DARK(c['skin'], 0.85)))
    # rosto: olho, sobrancelha, boca
    if cid == 'kai':
        p.line([(10, 43), (15, 42)], A((40, 30, 30)), 1.6)          # olho semicerrado
        p.line([(9, 39), (14, 37.5)], A((70, 52, 50), 150), 1.0)     # olheira
        p.line([(9, 48), (16, 48.5)], A((22, 20, 24)), 1.4)          # sobrancelha caída
        p.line([(14, 25), (18, 25.5)], A((110, 70, 64)), 1.1)
    elif cid == 'souta':
        p.ellipse(13, 44, 2.0, 1.6, A((28, 24, 22)))
        p.line([(9, 50), (17, 51)], A((60, 46, 36)), 1.6)            # sobrancelha firme
        p.line([(14, 26), (18, 27)], A((120, 76, 66)), 1.1)
    else:
        p.ellipse(13, 42, 1.7, 1.4, A((28, 24, 22)))                 # olhar baixo
        p.line([(10, 48), (16, 46)], A((120, 104, 80)), 1.2)
        p.line([(14, 25), (17, 24.5)], A((110, 72, 64)), 1.0)
    # sombra da franja sobre o rosto
    p.poly([(-6, 56), (8, 58), (19, 52), (21, 44), (14, 46), (4, 48), (-6, 46)], A(DARK(c['skin'], 0.72), 200))
    # cabelo
    if cid == 'kai':  # preto, bagunçado, franja cobrindo os olhos
        hair = [(-17, 34), (-20, 48), (-16, 62), (-6, 70), (6, 71), (16, 66), (22, 58), (23, 50), (19, 52),
                (20, 45), (15, 50), (13, 44), (9, 51), (5, 46), (2, 54), (-3, 50), (-8, 46), (-10, 38), (-14, 30)]
        p.poly(hair, A(c['hair']))
        for tip in [(-18, 66, -24, 70), (-6, 72, -4, 78), (8, 72, 14, 77), (18, 64, 25, 66)]:
            p.poly([(tip[0] - 3, tip[1] - 4), (tip[2], tip[3]), (tip[0] + 4, tip[1] - 3)], A(c['hair']), smooth=False)
    elif cid == 'souta':  # castanho, penteado de lado
        hair = [(-16, 32), (-18, 50), (-12, 64), (0, 70), (12, 69), (20, 62), (22, 55), (16, 57), (8, 58),
                (2, 55), (-6, 50), (-10, 40), (-13, 30)]
        p.poly(hair, A(c['hair']))
        p.line([(-8, 64), (6, 66), (16, 62)], A(DARK(c['hair'], 1.35), 200), 1.4, smooth=True)
    else:  # loiro acinzentado, franja comprida caindo no rosto
        hair = [(-17, 28), (-20, 46), (-15, 62), (-3, 70), (10, 69), (19, 62), (22, 52), (21, 42), (17, 46),
                (16, 38), (12, 46), (8, 40), (4, 50), (-4, 48), (-9, 40), (-12, 26)]
        p.poly(hair, A(c['hair']))
        p.line([(-10, 60), (4, 64), (16, 58)], A(DARK(c['hair'], 1.2), 200), 1.4, smooth=True)
        # óculos
        p.line([(8, 41), (17, 41), (17.5, 46), (8.5, 46), (8, 41)], A(c['glasses']), 1.4)
        p.line([(8, 44), (-1, 42)], A(c['glasses']), 1.1)
    return p.finish()


def limb(color, top_w, bot_w, length, cuff=None, hand=None, skin=None, back=False, fold=True):
    """Braço/perna pendurado do pivô (0,0) para baixo. Pontas arredondadas para a junta não aparecer."""
    extra = 20 if hand else 0
    r = max(top_w, bot_w) / 2
    p = Part((-r - 2, -length - extra - r - 2, r + 2, r + 2))
    tw, bw = top_w / 2, bot_w / 2
    p.ellipse(0, 0, tw, tw, A(color))
    p.poly([(-tw, 0), (-tw * 1.03, -length * 0.45), (-bw, -length), (bw, -length), (tw * 1.02, -length * 0.5), (tw, 0)], A(color))
    if not hand:
        p.ellipse(0, -length, bw, bw, A(color))
    if fold:
        p.line([(-tw * 0.2, -length * 0.25), (bw * 0.1, -length * 0.8)], A(DARK(color, 0.8), 120), 1.0)
    if cuff:
        p.poly([(-bw - 0.6, -length + 6), (bw + 0.6, -length + 6), (bw + 0.6, -length), (-bw - 0.6, -length)], A(cuff), smooth=False)
    if hand:
        p.poly([(-bw + 0.5, -length + 1), (-bw - 1, -length - 8), (-bw + 1, -length - 15), (2, -length - 18),
                (bw + 1.5, -length - 13), (bw + 1, -length - 4), (bw - 0.5, -length + 1)], A(skin))
        p.line([(bw - 1, -length - 7), (bw + 1.6, -length - 12)], A(DARK(skin, 0.75)), 1.0)
    return p.finish(back=back, top_rim=False)


def foot(c, back=False):
    p = Part((-12, -11, 30, 3))
    p.poly([(-8, 2), (-10, -4), (-9, -10), (24, -10), (28, -7), (27, -3), (20, 0), (8, 2)], A(c['shoe']))
    p.line([(-9, -10), (27, -10)], A((6, 6, 6)), 1.6)
    p.line([(4, 0), (18, -2)], A((70, 70, 74), 120), 1.0)  # brilho do couro
    return p.finish(back=back, rim=0.35, top_rim=False)


# medidas do esqueleto (px base)
SK = dict(thigh=80, shin=74, ankle=10, upper=58, fore=52, hip=(0, -2), shoulder=(-4, 110), neck=(1, 118))


def build(cid):
    c = C[cid]
    parts = {}
    sleeve = c['cardigan'] if cid == 'ren' else c['shirt']
    sleeve2 = c['cardigan2'] if cid == 'ren' else c['shirt2']
    cuff = c['shirt'] if cid == 'ren' else None
    parts['torso'] = torso(cid)
    parts['head'] = head(cid)
    for side, back in (('f', False), ('b', True)):
        parts[f'thigh_{side}'] = limb(c['pants'], 25, 18, SK['thigh'], back=back)
        parts[f'shin_{side}'] = limb(c['pants'], 18, 14, SK['shin'], back=back)
        parts[f'foot_{side}'] = foot(c, back=back)
        parts[f'upper_{side}'] = limb(sleeve if side == 'f' else sleeve2, 18, 14, SK['upper'], back=back)
        parts[f'fore_{side}'] = limb(sleeve if side == 'f' else sleeve2, 14, 11, SK['fore'], cuff=cuff, hand=True, skin=c['skin'], back=back)
    layout = {}
    for name, (img, pivot) in parts.items():
        fn = f'{cid}_{name}.png'
        img.save(f'{OUT}/{fn}')
        layout[name] = dict(file=fn, pivot=[round(pivot[0], 4), round(pivot[1], 4)])
    return layout, parts


if __name__ == '__main__':
    allj = {}
    for cid in ('kai', 'souta', 'ren'):
        layout, _ = build(cid)
        allj[cid] = layout
    allj['_skeleton'] = SK
    allj['_ppu'] = 100 * EXPORT
    json.dump(allj, open(f'{OUT}/rig_layout.json', 'w'), indent=1)
    print('ok', len(os.listdir(OUT)))
