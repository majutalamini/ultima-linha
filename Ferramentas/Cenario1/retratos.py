"""
Retratos do diálogo (Kai, Souta, Ren) no clima do jogo: busto em silhueta, quase todo na sombra,
com luz vermelha de emergência batendo de lado e um resto de luz fria fluorescente vindo de cima.
Gera out/Art/Cenario1/Retratos/{id}_retrato.png (620x1024, fundo transparente).
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops
from scipy.ndimage import gaussian_filter

OUT = 'out/Art/Cenario1/Retratos'
os.makedirs(OUT, exist_ok=True)
W, H = 620, 1024
S = 3
RED = np.array([214, 38, 30], np.float32)
COLD = np.array([150, 170, 178], np.float32)
rng = np.random.default_rng(7)


def catmull(pts, n=12):
    P = [pts[-1]] + list(pts) + [pts[0], pts[1]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = map(np.array, (P[i - 1], P[i], P[i + 1], P[i + 2]))
        for t in np.linspace(0, 1, n, endpoint=False):
            out.append(tuple(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                                    + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)))
    return out


def mask(polys=(), smooth=True, ellipses=(), lines=()):
    m = Image.new('L', (W * S, H * S), 0)
    d = ImageDraw.Draw(m)
    for p in polys:
        pts = catmull(p) if smooth else p
        d.polygon([(x * S, y * S) for x, y in pts], fill=255)
    for cx, cy, rx, ry in ellipses:
        d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=255)
    for pts, w in lines:
        d.line([(x * S, y * S) for x, y in pts], fill=255, width=int(w * S), joint='curve')
    return np.asarray(m).astype(np.float32) / 255


def blur(a, r):
    return gaussian_filter(np.asarray(a, np.float32), max(0.1, r * S))


def shift(a, dx, dy):
    dx, dy = int(dx * S), int(dy * S)
    out = np.zeros_like(a)
    h, w = a.shape
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = a[ys, xs]
    return out


def edge(a, dx, dy, soft=2.0):
    """Parte da forma que 'olha' para a direção (-dx, -dy): borda iluminada por esse lado."""
    return blur(np.clip(a - shift(a, dx, dy), 0, 1), soft) * a


class Canvas:
    def __init__(self):
        self.rgb = np.zeros((H * S, W * S, 3), np.float32)
        self.a = np.zeros((H * S, W * S), np.float32)

    def layer(self, m, color, red=1.0, cold=0.35, fill=0.22, round_r=22, rim_r=5, rim=1.0, **_):
        """Forma escura com volume + contorno vermelho nítido à esquerda + resto de luz fria por cima."""
        base = np.array(color, np.float32)[None, None, :] * np.ones((H * S, W * S, 1), np.float32)
        h = blur(m, round_r)
        gy, gx = np.gradient(h)
        k = round_r * S * 2.2
        nx, ny = -gx * k, -gy * k
        ln = np.sqrt(nx * nx + ny * ny + 1)
        nx, ny, nz = nx / ln, ny / ln, 1 / ln
        def lam(L):
            L = np.array(L, np.float32); L /= np.linalg.norm(L)
            return np.clip(nx * L[0] + ny * L[1] + nz * L[2], 0, 1)
        key = lam((-0.8, -0.3, 0.55)) ** 1.5 * fill
        shade = 1 - 0.45 * lam((0.85, 0.35, 0.35))
        # contorno: borda esquerda (vermelha, nítida + brilho largo) e borda de cima (fria)
        rim_l = blur(np.clip(m - shift(m, rim_r * 1.6, -rim_r * 0.2), 0, 1), rim_r * 0.35) * m
        rim_w = blur(np.clip(m - shift(m, rim_r * 5, 0), 0, 1), rim_r * 1.5) * m * 0.35
        rim_t = blur(np.clip(m - shift(m, -rim_r * 0.3, rim_r * 1.4), 0, 1), rim_r * 0.5) * m
        r = np.clip(rim_l + rim_w, 0, 1) * rim * red
        out = base * shade[..., None] + RED[None, None, :] * key[..., None]
        out = out * (1 - r[..., None]) + RED[None, None, :] * r[..., None]
        out = out + COLD[None, None, :] * (rim_t * cold * 0.55)[..., None]
        self.rgb = self.rgb * (1 - m[..., None]) + out * m[..., None]
        self.a = np.maximum(self.a, m)

    def paint(self, m, color, alpha=1.0):
        c = np.array(color, np.float32)[None, None, :]
        k = (m * alpha)[..., None]
        self.rgb = self.rgb * (1 - k) + c * k
        self.a = np.maximum(self.a, m * alpha)

    def glow(self, m, color, strength=1.0, radius=6):
        g = blur(m, radius) * strength
        c = np.array(color, np.float32)[None, None, :]
        self.rgb = self.rgb + c * g[..., None]
        self.a = np.maximum(self.a, np.clip(g, 0, 1))

    def over(self, other, scale, pivot):
        """Cola outra tela por cima, ampliada em volta de 'pivot' (cabeça um pouco maior que o corpo)."""
        from scipy.ndimage import affine_transform
        px, py = pivot[0] * S, pivot[1] * S
        inv = 1 / scale
        mat = np.array([[inv, 0], [0, inv]])
        off = np.array([py - py * inv, px - px * inv])
        a = affine_transform(other.a, mat, offset=off, order=1)
        rgb = np.dstack([affine_transform(other.rgb[..., c], mat, offset=off, order=1) for c in range(3)])
        self.rgb = self.rgb * (1 - a[..., None]) + rgb * a[..., None]
        self.a = np.maximum(self.a, a)

    def image(self, backglow=True, fade=0.16):
        rgb, a = self.rgb.copy(), self.a.copy()
        if backglow:  # halo vermelho fraco atrás da cabeça, separa a silhueta do fundo
            yy, xx = np.mgrid[0:H * S, 0:W * S].astype(np.float32)
            d = np.sqrt(((xx / S - 250) / 300) ** 2 + ((yy / S - 470) / 360) ** 2)
            g = np.clip(1 - d, 0, 1) ** 2 * 0.42
            under = g * (1 - a)
            out_a = a + under
            rgb = (rgb * a[..., None] + np.array([120, 14, 12], np.float32) * under[..., None]) / np.maximum(out_a, 1e-4)[..., None]
            a = out_a
        # granulado bem leve, parado (textura de impressão, não animado)
        n = rng.normal(0, 1, (H * S, W * S)).astype(np.float32)
        n = blur((n * 0.5 + 0.5) / 1.0, 0.5) - 0.5
        rgb = rgb * (1 + n[..., None] * 0.22)
        # some embaixo (o retrato "sai" da caixa de diálogo)
        yy = np.arange(H * S, dtype=np.float32)[:, None] / (H * S)
        a = a * np.clip((1 - yy) / fade, 0, 1)
        img = np.dstack([np.clip(rgb, 0, 255), np.clip(a, 0, 1) * 255]).astype(np.uint8)
        return Image.fromarray(img, 'RGBA').resize((W, H), Image.LANCZOS)


# ------------------------------------------------------------------ peças comuns
def lock(root, tip, width, bend=0.0, n=24):
    """Mecha de cabelo: curva que afina da raiz até a ponta. bend = curvatura (px, para o lado)."""
    r, t = np.array(root, float), np.array(tip, float)
    d = t - r
    L = np.linalg.norm(d) or 1
    perp = np.array([-d[1], d[0]]) / L
    mid = (r + t) / 2 + perp * bend
    left, right = [], []
    for i in range(n + 1):
        u = i / n
        p = (1 - u) ** 2 * r + 2 * (1 - u) * u * mid + u ** 2 * t
        tan = 2 * (1 - u) * (mid - r) + 2 * u * (t - mid)
        tan /= (np.linalg.norm(tan) or 1)
        nrm = np.array([-tan[1], tan[0]])
        w = width * 0.5 * (1 - u) ** 0.8
        left.append(tuple(p + nrm * w))
        right.append(tuple(p - nrm * w))
    return left + right[::-1]


def locks_mask(locks, dy=0):
    return mask([[(x, y + dy) for x, y in lock(*l)] for l in locks], smooth=False)


def torso_shape(drop=0, hunch=0):
    d = drop
    return [(-40, 1080), (-30, 900 + d), (20, 822 + d), (110, 772 + d + hunch), (222, 728 + d), (300, 716 + d),
            (390, 722 + d), (486, 748 + d), (566, 796 + d), (636, 880 + d), (660, 1080)]


def neck_shape(t=0):
    return [(270, 540 + t), (364, 540 + t), (370, 640), (384, 726), (318, 744), (252, 726), (266, 640)]


HEAD = [(214, 392), (222, 314), (258, 262), (318, 238), (380, 250), (424, 296), (440, 360), (440, 414),
        (452, 458), (446, 474), (436, 486), (430, 520), (408, 560), (366, 590), (322, 596), (280, 576),
        (248, 540), (228, 492), (218, 446)]


def head_shape(t=0):
    return [(x, y + t) for x, y in HEAD]


def ear(t=0):
    return [(224, 424 + t), (202, 414 + t), (194, 444 + t), (204, 480 + t), (230, 488 + t)]


def eye(cv, cx, cy, w, h, lid=0.5, iris=(24, 16, 16), spec=(170, 180, 186), spec_a=0.7, white=(62, 44, 44), lash=(8, 6, 7)):
    m_eye = mask(ellipses=[(cx, cy, w, h)])
    lidm = mask(polys=[[(cx - w - 4, cy - h - 30), (cx + w + 4, cy - h - 30), (cx + w + 4, cy - h + 2 * h * lid),
                        (cx - w - 4, cy - h + 2 * h * lid)]], smooth=False)
    vis = np.clip(m_eye - lidm, 0, 1)
    cv.paint(vis, white, 0.9)
    cv.paint(vis * mask(ellipses=[(cx + w * 0.12, cy + h * 0.15, h * 1.0, h * 1.0)]), iris, 1.0)
    ly = cy - h + 2 * h * lid
    cv.paint(mask(lines=[([(cx - w - 3, ly + 1), (cx, ly - 1.5), (cx + w + 4, ly - 1)], 3.6)]), lash, 1.0)
    if spec_a > 0:
        cv.paint(vis * mask(ellipses=[(cx + w * 0.3, (ly + cy + h) / 2 - 1, 2.6, 2.6)]), spec, spec_a)


def face_shadow(cv, t, hm, strength=0.6, top=410, bottom=470):
    """Sombra que a franja joga no rosto."""
    sh = blur(mask([[(220, top + t), (452, top - 10 + t), (450, bottom - 10 + t), (220, bottom + t)]]), 14)
    cv.paint(sh * mask([head_shape(t)]) * (1 - hm), (4, 2, 3), strength)


def nose_mouth(cv, t, mouth=((354, 546), (386, 544)), mouth_c=(30, 16, 16)):
    cv.paint(mask(lines=[([(438, 470 + t), (428, 488 + t), (414, 490 + t)], 2.6)]), (18, 10, 10), 0.7)
    cv.paint(mask(lines=[([(mouth[0][0], mouth[0][1] + t), (mouth[1][0], mouth[1][1] + t)], 3)]), mouth_c, 0.9)


def chin_shadow(cv, t):
    cv.paint(blur(mask([head_shape(t)]), 10) * mask([neck_shape(t)]) * 0 + blur(mask([[ (250, 560 + t), (420, 560 + t), (400, 640 + t), (270, 640 + t)]]), 16) * mask([neck_shape(t)]), (4, 3, 3), 0.75)


def body_fade(cv, m, k=0.55):
    """Escurece o corpo para baixo: a luz está no rosto."""
    yy = np.arange(H * S, dtype=np.float32)[:, None] / (H * S)
    f = np.clip((yy - 0.72) / 0.28, 0, 1) * k
    cv.rgb *= (1 - f * m)[..., None] if m is not None else (1 - f)[..., None]


# ------------------------------------------------------------------ personagens
def kai():
    """Cansado: cabeça baixa, cabelo preto bagunçado cobrindo os olhos, gravata frouxa e torta."""
    cv = Canvas()
    t = 16 + 60
    torso = mask([torso_shape(drop=8, hunch=10)])
    cv.layer(torso, (30, 29, 31), fill=0.07, round_r=40)
    collar = mask([[(256, 728), (318, 776), (302, 830), (234, 750)], [(378, 726), (334, 774), (356, 826), (406, 748)]])
    cv.layer(collar, (38, 37, 39), fill=0.06, round_r=8, rim_r=3)
    tie = mask([[(298, 770), (334, 766), (326, 1080), (276, 1080), (288, 860)]])
    cv.layer(tie, (13, 13, 18), fill=0.1, round_r=10, rim_r=3, rim=0.6)
    cv.layer(mask([[(294, 752), (338, 750), (334, 784), (298, 788)]]), (15, 15, 20), fill=0.1, round_r=6, rim_r=3, rim=0.6)
    body_fade(cv, np.clip(torso + tie, 0, 1))
    neck = mask([neck_shape(t)])
    cv.layer(neck, (30, 22, 20), fill=0.16, round_r=18, rim=0.8)
    chin_shadow(cv, t)
    hv = Canvas()
    hv.layer(mask([ear(t)]), (34, 25, 23), fill=0.25, round_r=8)
    head = mask([head_shape(t)])
    hv.layer(head, (36, 27, 25), fill=0.30, round_r=30)
    # olhos semicerrados, olheiras
    hv.paint(blur(mask(ellipses=[(318, 486 + t, 26, 9), (404, 484 + t, 17, 8)]), 4), (22, 10, 12), 0.6)
    eye(hv, 318, 466 + t, 20, 9, lid=0.6, spec_a=0.25)
    eye(hv, 404, 464 + t, 13, 8, lid=0.6, spec_a=0.2)
    nose_mouth(hv, t, mouth=((352, 550), (382, 552)))
    # cabelo: calota + mechas bagunçadas
    cap = mask(ellipses=[(326, 340 + t, 126, 100)])
    locks = [((270, 300), (172, 430), 104, 26), ((250, 350), (190, 492), 74, 12), ((290, 282), (190, 262), 92, -34),
             ((318, 278), (296, 214), 70, -16), ((350, 278), (436, 206), 84, 24), ((382, 296), (488, 326), 84, -14),
             ((404, 330), (476, 430), 74, 14), ((300, 330), (256, 486), 84, 12), ((332, 340), (318, 506), 72, -10),
             ((362, 340), (386, 494), 70, 10), ((392, 340), (446, 470), 62, -12), ((230, 330), (166, 352), 60, -6)]
    hm = np.clip(cap + locks_mask(locks, t), 0, 1)
    face_shadow(hv, t, hm, 0.75, 420, 490)
    hv.layer(hm, (12, 11, 14), fill=0.14, round_r=12, rim_r=4, cold=0.45)
    cv.over(hv, 1.15, (322, 600 + t))
    return cv.image()


def souta():
    """Seguro e frio: postura reta, queixo erguido, cabelo penteado de lado, gravata impecável."""
    cv = Canvas()
    t = -4 + 58
    torso = mask([torso_shape(drop=-6)])
    cv.layer(torso, (40, 39, 38), fill=0.07, round_r=40)
    collar = mask([[(254, 714), (318, 752), (306, 806), (236, 736)], [(382, 714), (322, 752), (334, 806), (402, 736)]])
    cv.layer(collar, (50, 49, 48), fill=0.06, round_r=8, rim_r=3)
    tie = mask([[(304, 752), (336, 752), (352, 1080), (288, 1080)]])
    cv.layer(tie, (12, 13, 20), fill=0.1, round_r=10, rim_r=3, rim=0.6)
    cv.layer(mask([[(302, 734), (338, 734), (336, 764), (304, 764)]]), (14, 15, 22), fill=0.1, round_r=6, rim_r=3, rim=0.6)
    cv.paint(mask(polys=[[(282, 920), (360, 918), (360, 928), (282, 930)]], smooth=False), (120, 120, 124), 0.7)  # prendedor
    cv.paint(mask(polys=[[(282, 920), (300, 920), (300, 930), (282, 930)]], smooth=False), (230, 70, 60), 0.6)
    body_fade(cv, np.clip(torso + tie, 0, 1))
    neck = mask([neck_shape(t)])
    cv.layer(neck, (32, 24, 21), fill=0.16, round_r=18, rim=0.8)
    chin_shadow(cv, t)
    hv = Canvas()
    hv.layer(mask([ear(t)]), (36, 27, 24), fill=0.25, round_r=8)
    head = mask([head_shape(t)])
    hv.layer(head, (40, 30, 27), fill=0.32, round_r=30)
    # olhar firme, brilho frio nos olhos
    eye(hv, 318, 452 + t, 20, 9, lid=0.32, spec=(200, 214, 220), spec_a=0.9)
    eye(hv, 404, 450 + t, 13, 8, lid=0.32, spec=(200, 214, 220), spec_a=0.8)
    hv.paint(mask(lines=[([(290, 424 + t), (318, 418 + t), (346, 420 + t)], 6), ([(386, 418 + t), (426, 422 + t)], 5)]), (12, 9, 9))
    nose_mouth(hv, t, mouth=((350, 548), (388, 547)))
    # cabelo penteado de lado, testa à mostra
    cap = mask([[(204, 450), (198, 350), (222, 284), (268, 242), (330, 226), (392, 236), (440, 272), (460, 330), (458, 374),
                 (432, 346), (392, 326), (348, 322), (302, 334), (266, 362), (246, 404), (228, 456)]])
    cap = np.asarray(Image.fromarray((cap * 255).astype(np.uint8)).transform(cap.shape[::-1], Image.AFFINE, (1, 0, 0, 0, 1, -t * S))).astype(np.float32) / 255
    locks = [((300, 300), (476, 352), 70, -26), ((260, 290), (430, 300), 60, -30), ((236, 330), (214, 452), 40, 8)]
    hm = np.clip(cap + locks_mask(locks, t), 0, 1)
    face_shadow(hv, t, hm, 0.35, 360, 400)
    hv.layer(hm, (30, 23, 18), fill=0.16, round_r=14, rim_r=4, cold=0.55)
    hv.paint(mask(lines=[([(244, 312 + t), (330, 280 + t), (430, 296 + t)], 2.5)]) * hm, (64, 50, 40), 0.5)  # risca
    cv.over(hv, 1.15, (322, 600 + t))
    return cv.image()


def ren():
    """Nervoso: encolhido, cabeça baixa, franja comprida, óculos refletindo a luz vermelha, cardigã."""
    cv = Canvas()
    t = 22 + 58
    torso = mask([torso_shape(drop=24, hunch=20)])
    cv.layer(torso, (36, 35, 34), fill=0.07, round_r=40)
    vee = mask([[(232, 752), (300, 1090), (352, 1090), (404, 752), (318, 770)]], smooth=False)
    cardigan = np.clip(blur(torso, 3) * 1.6, 0, 1) * (1 - vee)
    cv.layer(cardigan, (24, 18, 15), fill=0.08, round_r=26, rim_r=5)
    cv.paint(mask(lines=[([(244, 780), (298, 1080)], 3), ([(392, 780), (356, 1080)], 3)]), (10, 7, 6), 0.8)
    for y in (880, 960, 1040):  # botões
        cv.paint(mask(ellipses=[(286 + (y - 780) * 0.05, y, 5, 5)]), (60, 46, 38), 0.6)
    tie = mask([[(308, 770), (334, 768), (342, 1080), (300, 1080)]])
    cv.layer(tie, (14, 14, 18), fill=0.1, round_r=8, rim_r=3, rim=0.5)
    body_fade(cv, np.clip(torso + cardigan + tie, 0, 1))
    neck = mask([neck_shape(t)])
    cv.layer(neck, (30, 23, 21), fill=0.16, round_r=18, rim=0.8)
    chin_shadow(cv, t)
    hv = Canvas()
    hv.layer(mask([ear(t)]), (34, 26, 24), fill=0.25, round_r=8)
    head = mask([head_shape(t)])
    hv.layer(head, (38, 29, 27), fill=0.30, round_r=30)
    eye(hv, 318, 470 + t, 17, 9, lid=0.42, spec_a=0.5)
    eye(hv, 402, 468 + t, 12, 8, lid=0.42, spec_a=0.45)
    hv.paint(mask(lines=[([(296, 440 + t), (342, 428 + t)], 4), ([(388, 428 + t), (420, 438 + t)], 3.5)]), (26, 20, 16))
    nose_mouth(hv, t, mouth=((358, 556), (380, 554)))
    # cabelo claro, liso, franja comprida
    cap = mask(ellipses=[(328, 330 + t, 132, 102)])
    locks = [((240, 330), (200, 470), 70, 14), ((222, 360), (210, 500), 46, 6), ((280, 360), (272, 470), 62, -6),
             ((318, 366), (322, 480), 60, 6), ((360, 364), (380, 474), 58, -6), ((400, 356), (436, 456), 54, 6),
             ((430, 340), (466, 420), 44, -4), ((250, 300), (196, 400), 70, 16), ((420, 300), (470, 380), 60, -10)]
    hm = np.clip(cap + locks_mask(locks, t), 0, 1)
    face_shadow(hv, t, hm, 0.7, 430, 500)
    hv.layer(hm, (46, 40, 30), fill=0.14, round_r=12, rim_r=4, cold=0.5)
    # óculos: aro fino, lentes com reflexo vermelho
    lensL = [(284, 450 + t), (350, 450 + t), (348, 492 + t), (290, 494 + t)]
    lensR = [(382, 450 + t), (428, 450 + t), (426, 490 + t), (386, 492 + t)]
    lens = np.clip(mask([lensL, lensR]), 0, 1)
    yy, xx = np.mgrid[0:H * S, 0:W * S].astype(np.float32)
    u = xx / S + yy / S * 0.7
    streak = np.clip(1 - np.abs(u - (640 + t * 0.7)) / 16, 0, 1) + np.clip(1 - np.abs(u - (676 + t * 0.7)) / 6, 0, 1) * 0.8 \
        + np.clip(1 - np.abs(u - (744 + t * 0.7)) / 12, 0, 1) * 0.9
    hv.paint(lens, (52, 12, 12), 0.55)
    hv.paint(lens * np.clip(streak, 0, 1), (240, 80, 66), 0.85)
    frame = mask(lines=[(lensL + [lensL[0]], 3.5), (lensR + [lensR[0]], 3.5), ([(350, 462 + t), (382, 462 + t)], 3),
                        ([(284, 458 + t), (222, 448 + t)], 3.5)])
    frame = np.clip(frame, 0, 1)
    hv.paint(frame, (16, 12, 12))
    hv.paint(frame * blur(np.clip(frame - shift(frame, 2, 2), 0, 1), 0.8), (230, 64, 52), 1.0)
    for pts in ([(330, 262), (280, 330), (262, 400), (256, 470)], [(334, 262), (318, 360), (322, 470)],
                [(338, 262), (366, 360), (380, 466)], [(342, 264), (404, 330), (430, 440)], [(300, 270), (236, 340), (210, 440)]):
        hv.paint(blur(mask(lines=[([(x, y + t) for x, y in pts], 2.2)]), 0.8) * hm, (20, 16, 12), 0.6)
    cv.over(hv, 1.15, (322, 600 + t))
    return cv.image()


if __name__ == '__main__':
    imgs = {}
    for name, fn in (('kai', kai), ('souta', souta), ('ren', ren)):
        im = fn()
        im.save(f'{OUT}/{name}_retrato.png')
        imgs[name] = im
    prev = Image.new('RGBA', (W * 3 + 40, H), (12, 11, 12, 255))
    for i, n in enumerate(('kai', 'souta', 'ren')):
        prev.alpha_composite(imgs[n], (i * (W + 20), 0))
    prev.convert('RGB').save('retratos/previa_retratos.png')
