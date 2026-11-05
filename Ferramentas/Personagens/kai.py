"""Constrói o Kai do zero. blender --background --python kai.py -- <saida.blend>

Medidas tiradas da ilustração de corpo inteiro (809 px/m): 1,78 m, cabeça 0,206 m (8,6 cabeças),
ombros 0,44 m, cinto a 1,05 m, virilha a 0,86 m, joelho a 0,525 m.
"""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
import lib  # noqa: E402

BUILD = os.path.join(AQUI, "build")
QUEIXO, COROA = 1.55, 1.755


def lados(fn):
    for s, n in ((1, "L"), (-1, "R")):
        fn(s, n)


def esqueleto():
    sp = [
        ("hips", (0, 0, 0.96), (0, 0, 1.06), None, False),
        ("spine", (0, 0, 1.06), (0, 0, 1.24), "hips", True),
        ("chest", (0, 0, 1.24), (0, 0.004, 1.46), "spine", True),
        ("neck", (0, 0.004, 1.46), (0, -0.008, 1.565), "chest", True),
        ("head", (0, -0.008, 1.565), (0, -0.008, 1.755), "neck", True),
        ("tie", (0.012, -0.135, 1.39), (0.03, -0.14, 1.04), "chest", False),
    ]
    for s, n in ((1, "L"), (-1, "R")):
        S = Vector((0.185 * s, 0.005, 1.455))
        d1 = Vector((0.29 * s, 0.0, -0.957)).normalized()
        E = S + d1 * 0.30
        d2 = Vector((0.29 * s, -0.09, -0.95)).normalized()
        W = E + d2 * 0.27
        H = W + d2 * 0.09
        sp += [
            (f"clav.{n}", (0.03 * s, 0.0, 1.44), tuple(S), "chest", False),
            (f"upper.{n}", tuple(S), tuple(E), f"clav.{n}", True),
            (f"fore.{n}", tuple(E), tuple(W), f"upper.{n}", True),
            (f"hand.{n}", tuple(W), tuple(H), f"fore.{n}", True),
            (f"thigh.{n}", (0.085 * s, 0, 0.93), (0.09 * s, -0.014, 0.525), "hips", False),
            (f"shin.{n}", (0.09 * s, -0.014, 0.525), (0.095 * s, 0.0, 0.095), f"thigh.{n}", True),
            (f"foot.{n}", (0.095 * s, 0.0, 0.095), (0.095 * s, -0.125, 0.03), f"shin.{n}", True),
            (f"toe.{n}", (0.095 * s, -0.125, 0.03), (0.095 * s, -0.20, 0.03), f"foot.{n}", True),
        ]
    return lib.Bones(sp)


PERFIL = [
    (0.000, -0.0860, -0.0640, 0.0090), (0.030, -0.0915, -0.0340, 0.0190), (0.090, -0.0925, 0.0000, 0.0325),
    (0.170, -0.0945, 0.0240, 0.0445), (0.270, -0.0975, 0.0440, 0.0545), (0.360, -0.0995, 0.0600, 0.0615),
    (0.450, -0.0985, 0.0750, 0.0675), (0.520, -0.0945, 0.0850, 0.0720), (0.620, -0.0965, 0.0950, 0.0770),
    (0.750, -0.0920, 0.1000, 0.0790), (0.870, -0.0720, 0.0920, 0.0700), (0.950, -0.0400, 0.0650, 0.0460),
    (1.000, -0.0050, 0.0250, 0.0120),
]


def frente(x, z):
    """Y da superfície frontal da cabeça em (x, z): as mechas da franja correm rente a ela."""
    t = max(0.0, min(1.0, (z - QUEIXO) / (COROA - QUEIXO)))
    for (t0, f0, b0, a0), (t1, f1, b1, a1) in zip(PERFIL, PERFIL[1:]):
        if t <= t1:
            k = (t - t0) / (t1 - t0)
            fr, tr, a = f0 + (f1 - f0) * k, b0 + (b1 - b0) * k, a0 + (a1 - a0) * k
            break
    u = min(1.0, abs(x) / max(a, 1e-4))
    cy, r = (fr + tr) / 2, (tr - fr) / 2
    return cy - r * math.sqrt(max(0.0, 1 - u * u))


def cabeca(p):
    wh = {"head": 1.0}
    H = COROA - QUEIXO
    # t, frente, trás, meia-largura (perfil tirado da proporção do rosto na referência)
    est = []
    for t, fr, tr, a in PERFIL:
        z = QUEIXO + t * H
        cy = (fr + tr) / 2
        r = (tr - fr) / 2
        est.append(lib.st((0, cy, z), a, r, r, w=wh, e=2.25 if t < 0.5 else 2.0))
    p.loft(est, n=48, sub=4)
    # nariz pequeno e reto
    p.loft([lib.st((0, -0.0915, QUEIXO + 0.53 * H), 0.0065, 0.008, w=wh),
            lib.st((0, -0.1, QUEIXO + 0.44 * H), 0.0052, 0.0065, w=wh),
            lib.st((0, -0.1085, QUEIXO + 0.355 * H), 0.0062, 0.0058, w=wh),
            lib.st((0, -0.1, QUEIXO + 0.32 * H), 0.0058, 0.0075, w=wh)], n=12, sub=4)
    # orelhas
    lados(lambda s, n: p.ellipsoid((0.0725 * s, 0.018, QUEIXO + 0.47 * H), (0.0055, 0.016, 0.027), wh,
                                   rot=Matrix.Rotation(math.radians(12 * s), 3, "Y")))
    # pescoço longo e fino, base alargada em direção ao trapézio
    wn = {"neck": 1.0}
    p.loft([lib.st((0, 0.012, 1.405), 0.135, 0.085, w={"chest": 1}), lib.st((0, 0.012, 1.45), 0.095, 0.07, w={"chest": 0.6, "neck": 0.4}),
            lib.st((0, 0.008, 1.50), 0.0475, 0.05, w=wn), lib.st((0, 0.0, 1.56), 0.0415, 0.0445, w={"neck": 0.5, "head": 0.5}),
            lib.st((0, 0.0, 1.60), 0.04, 0.043, w=wh)], n=28, sub=4)


def maos(p, b):
    def um(s, n):
        S, E = b.pos[f"upper.{n}"]
        W = b.pos[f"fore.{n}"][1]
        Hh = b.pos[f"hand.{n}"][1]
        d = (Hh - W).normalized()
        up, fo, ha = {f"upper.{n}": 1}, {f"fore.{n}": 1}, {f"hand.{n}": 1}
        p.loft([lib.st(E + (S - E).normalized() * 0.03, 0.04, 0.041, w={f"upper.{n}": 0.5, f"fore.{n}": 0.5}, side=(0, 1, 0)),
                lib.st(E + (W - E) * 0.25, 0.0405, 0.0395, w=fo, side=(0, 1, 0)),
                lib.st(E + (W - E) * 0.6, 0.0345, 0.031, w=fo, side=(0, 1, 0)),
                lib.st(W - d * 0.01, 0.0265, 0.0215, w={f"fore.{n}": 0.7, f"hand.{n}": 0.3}, side=(0, 1, 0)),
                lib.st(W + d * 0.02, 0.0325, 0.0185, w=ha, side=(0, 1, 0)),
                lib.st(W + d * 0.07, 0.0415, 0.0165, w=ha, side=(0, 1, 0)),
                lib.st(W + d * 0.097, 0.0385, 0.0125, w=ha, side=(0, 1, 0))], n=24, sub=4)
        lateral = Vector((0, 1, 0))
        palma = d.cross(lateral).normalized() * (1 if s > 0 else -1)
        if palma.x * s > 0:
            palma = -palma
        for k, (off, ln) in enumerate(((-0.0285, 0.056), (-0.0095, 0.072), (0.0095, 0.078), (0.0285, 0.070))):
            base = W + d * 0.09 + lateral * off
            pts, cur, ang = [base], base, 0.2
            seg = ln / 3
            for j in range(3):
                ang += (0.55, 0.65, 0.5)[j]
                dirv = (d * math.cos(ang) + palma * math.sin(ang)).normalized()
                cur = cur + dirv * seg
                pts.append(cur)
            r = (0.0088, 0.0082, 0.0072, 0.0058)
            p.loft([lib.st(q, r[i], r[i], w=ha, side=(0, 1, 0)) for i, q in enumerate(pts)], n=10, sub=3)
        # polegar
        th0 = W + d * 0.018 - lateral * 0.03 + palma * 0.008
        th1 = th0 + (d * 0.75 + palma * 0.3 - lateral * 0.45).normalized() * 0.042
        th2 = th1 + (d * 0.85 + palma * 0.5 - lateral * 0.1).normalized() * 0.036
        p.loft([lib.st(th0, 0.0135, 0.0125, w=ha, side=(0, 1, 0)), lib.st(th1, 0.0105, 0.0095, w=ha, side=(0, 1, 0)),
                lib.st(th2, 0.0075, 0.007, w=ha, side=(0, 1, 0))], n=10, sub=3)

    lados(um)


def mecha_frente(p, q0, q1, q2, wd, wh, folga=0.006, sobe=0.0):
    """Mecha da franja: Bézier em (x, z) sobre o rosto, afastada da pele pela folga."""
    seg, pts = 14, []
    for i in range(seg + 1):
        t = i / seg
        x = (1 - t) ** 2 * q0[0] + 2 * t * (1 - t) * q1[0] + t * t * q2[0]
        z = (1 - t) ** 2 * q0[1] + 2 * t * (1 - t) * q1[1] + t * t * q2[1]
        y = frente(x, z) - folga - 0.012 * math.sin(math.pi * t) - sobe * t * t
        pts.append(Vector((x, y, z)))
    est = []
    c = Vector((0, 0.02, 1.665))
    for i, q in enumerate(pts):
        t = i / seg
        w = wd * min(1.0, 0.5 + t * 3.0) * (1 - t ** 2.0) + 0.0009
        est.append(dict(p=q, a=w, f=w * 0.5, b=w * 0.5, e=2.0, w=wh, side=lib._side_of(pts, i, (q - c).normalized())))
    p.loft(est, n=8, sub=1)


def cabelo(p):
    sk = lib.Skull((0, 0.002, 1.665), (0.0835, 0.1085, 0.0985))
    wh = {"head": 1.0}
    r = random.Random(11)

    # touca sob as mechas: nunca deixa o couro aparecer
    def hairline(lon):
        c = math.cos(math.radians(lon))
        return 40 + 14 * c if c > 0 else 12 + 66 * c

    verts, faces = [], []
    nlon, nlat = 56, 14
    for i in range(nlat + 1):
        for j in range(nlon):
            lon = 360 * j / nlon
            lat = hairline(lon) + (90 - hairline(lon)) * i / nlat
            verts.append(sk.at(lat, lon, 0.014))
    for i in range(nlat):
        for j in range(nlon):
            a, b = i * nlon + j, i * nlon + (j + 1) % nlon
            faces.append((a, b, b + nlon, a + nlon))
    p.add(verts, faces, [p._weight(wh)] * len(verts))

    # massa principal: tudo nasce do redemoinho no topo e corre pelo crânio; pontas irregulares abrem a silhueta
    n = 150
    for i in range(n):
        lon = (i * 137.5) % 360
        c = math.cos(math.radians(lon))
        lon_rel = (lon + 180) % 360 - 180
        if abs(lon_rel) < 42:  # a frente é da franja
            continue
        if c > 0:  # têmporas
            esq = lon_rel > 0
            lat1 = r.uniform(-8, 12) if esq else r.uniform(-26, -2)
        else:  # lados de trás e nuca
            lat1 = r.uniform(-14, 8) + c * r.uniform(30, 52)
        lat0 = r.uniform(78, 88)
        lib.lock(p, sk, (lat0, lon + r.uniform(-14, 14)), (lat1, lon + r.uniform(-11, 11)), r.uniform(0.0085, 0.0145), wh,
                 lift=0.045, belly=r.uniform(0.012, 0.03), flick=r.uniform(0.02, 0.17), hang=r.uniform(0.0, 0.008), thick=0.5)
    # camada de cima, mais curta, dá o volume bagunçado
    for i in range(46):
        lon = r.uniform(0, 360)
        lib.lock(p, sk, (r.uniform(80, 89), lon), (r.uniform(34, 62), lon + r.uniform(-22, 22)), r.uniform(0.0075, 0.0125), wh,
                 lift=0.06, belly=r.uniform(0.02, 0.05), flick=r.uniform(0.03, 0.13), thick=0.5)
    # fios soltos saindo do topo, como na ilustração
    for i in range(14):
        lon = r.uniform(0, 360)
        lib.lock(p, sk, (r.uniform(72, 86), lon), (r.uniform(28, 66), lon + r.uniform(-50, 50)), r.uniform(0.0016, 0.0028), wh,
                 lift=0.07, belly=r.uniform(0.08, 0.15), flick=r.uniform(0.1, 0.28), n=6)

    px = 0.012  # risca
    # varrida grande para o lado direito dele (-X): cobre a sobrancelha e chega à maçã do rosto
    for i in range(13):
        k = i / 12
        q0 = (px - 0.006 - 0.042 * k, 1.752 - 0.016 * k)
        q1 = (-0.022 - 0.036 * k + r.uniform(-0.006, 0.006), 1.70 - 0.012 * k)
        q2 = (-0.038 - 0.042 * k + r.uniform(-0.006, 0.006), 1.653 - 0.058 * (1 - k) ** 1.2 + r.uniform(-0.01, 0.012))
        mecha_frente(p, q0, q1, q2, r.uniform(0.0075, 0.0125), wh, folga=0.005 + 0.003 * k, sobe=0.012)
    # varrida menor para o lado esquerdo dele (+X): deixa o olho à mostra
    for i in range(10):
        k = i / 9
        q0 = (px + 0.006 + 0.04 * k, 1.752 - 0.016 * k)
        q1 = (0.03 + 0.03 * k + r.uniform(-0.005, 0.005), 1.712 - 0.006 * k)
        q2 = (0.056 + 0.026 * k + r.uniform(-0.004, 0.006), 1.674 - 0.03 * k + r.uniform(-0.008, 0.008))
        mecha_frente(p, q0, q1, q2, r.uniform(0.007, 0.0115), wh, folga=0.005, sobe=0.008)
    # fios finos caindo da risca entre os olhos
    for q2, q1, wd in (((-0.006, 1.622), (0.0, 1.69), 0.0048), ((0.006, 1.64), (0.012, 1.70), 0.0036), ((-0.018, 1.612), (-0.008, 1.68), 0.0042)):
        mecha_frente(p, (px, 1.752), q1, q2, wd, wh, folga=0.007, sobe=0.012)


def camisa(p, b):
    sp = {"spine": 1}
    ch = {"chest": 1}
    # tronco folgado; acima do cinto a camisa "blusa" um pouco
    p.loft([lib.st((0, 0.003, 0.88), 0.176, 0.128, 0.128, w={"hips": 1}, e=2.3),
            lib.st((0, 0.003, 0.97), 0.176, 0.127, 0.127, w={"hips": 1}, e=2.3),
            lib.st((0, 0.003, 1.03), 0.176, 0.126, 0.126, w={"hips": 0.6, "spine": 0.4}, e=2.3),
            lib.st((0, 0.0, 1.07), 0.178, 0.127, 0.125, w={"hips": 0.2, "spine": 0.8}, e=2.3),
            lib.st((0, 0.0, 1.14), 0.168, 0.118, 0.112, w=sp, e=2.3),
            lib.st((0, 0.0, 1.26), 0.176, 0.123, 0.112, w={"spine": 0.4, "chest": 0.6}, e=2.4),
            lib.st((0, 0.005, 1.36), 0.187, 0.125, 0.116, w=ch, e=2.5),
            lib.st((0, 0.008, 1.43), 0.178, 0.104, 0.108, w=ch, e=2.5),
            lib.st((0, 0.012, 1.475), 0.125, 0.082, 0.09, w=ch, e=2.2),
            lib.st((0, 0.012, 1.505), 0.062, 0.06, 0.066, w=ch)], n=44, sub=5)

    def manga(s, n):
        S = b.pos[f"upper.{n}"][0]
        E = b.pos[f"upper.{n}"][1]
        W = b.pos[f"fore.{n}"][1]
        fim = E + (W - E) * 0.40  # manga dobrada logo abaixo do cotovelo
        up, fo = {f"upper.{n}": 1}, {f"fore.{n}": 1}
        est = [lib.st(S + (S - E).normalized() * 0.012, 0.05, 0.06, w={f"clav.{n}": 0.5, "chest": 0.5}, side=(0, 1, 0)),
               lib.st(S + (E - S) * 0.14, 0.0695, 0.077, w=up, side=(0, 1, 0)),
               lib.st(S + (E - S) * 0.6, 0.0645, 0.066, w=up, side=(0, 1, 0)),
               lib.st(E, 0.0605, 0.0595, w={f"upper.{n}": 0.5, f"fore.{n}": 0.5}, side=(0, 1, 0)),
               lib.st(E + (W - E) * 0.25, 0.0565, 0.054, w=fo, side=(0, 1, 0)),
               lib.st(fim, 0.054, 0.052, w=fo, side=(0, 1, 0))]
        p.loft(est, n=24, sub=4)
        # rolo da manga: anéis grossos e tortos
        d = (W - E).normalized()
        for k, (o, ga) in enumerate(((0.0, 0.0615), (0.024, 0.0585))):
            c = fim + d * o
            p.loft([lib.st(c - d * 0.016, ga - 0.005, ga - 0.008, w=fo, side=(0, 1, 0)),
                    lib.st(c, ga + 0.002, ga, w=fo, side=(0, 1, 0)),
                    lib.st(c + d * 0.018, ga - 0.006, ga - 0.009, w=fo, side=(0, 1, 0))], n=24, sub=3)

    lados(manga)


def decote(co):
    """Abre o V do colarinho desabotoado e recorta a barra: presa no cinto de um lado, solta em ponta do outro."""
    if co.y < 0 and co.z > 1.385 and abs(co.x + 0.006) < (co.z - 1.385) * 0.62:
        return True
    th = math.degrees(math.atan2(co.x, -co.y))
    frente_ = math.exp(-(((th - 24) / 34) ** 2))
    tras = math.exp(-(((abs(th) - 152) * (1 if th < 0 else 3) / 42) ** 2))
    ponta = abs(th - 15) / 70
    return co.z < 1.062 - 0.15 * frente_ * (1 - ponta * 0.5) - 0.07 * tras - 0.004 * math.sin(th * 0.21)


def colarinho(p):
    ch = {"chest": 1}

    def asa(s, n):
        a = [(0.0, 0.08, 1.532), (0.05 * s, 0.056, 1.534), (0.066 * s, 0.0, 1.522), (0.056 * s, -0.062, 1.482), (0.03 * s, -0.106, 1.428)]
        bb = [(0.0, 0.108, 1.482), (0.086 * s, 0.09, 1.478), (0.124 * s, 0.012, 1.468), (0.13 * s, -0.082, 1.43), (0.088 * s, -0.142, 1.382)]
        p.plate(a, bb, ch, esp=0.0035)

    lados(asa)
    # bolso no peito esquerdo dele
    z0, z1 = 1.262, 1.342
    n = 7
    xs = [0.062 + 0.09 * i / (n - 1) for i in range(n)]
    p.plate([(x, -0.1365 + (x - 0.05) ** 2 * 1.4, z1) for x in xs], [(x, -0.1385 + (x - 0.05) ** 2 * 1.4, z0) for x in xs], ch, esp=0.0014)
    # botões
    for z in (1.31, 1.22, 1.13):
        p.ellipsoid((-0.004, -0.1372, z), (0.0062, 0.003, 0.0062), {"chest" if z > 1.25 else "spine": 1}, seg=10, rings=6)


def gravata(p):
    wch, wtie = {"chest": 1}, {"tie": 1}
    # alça frouxa em volta do pescoço, por dentro do colarinho
    for s in (1, -1):
        a = [(0.05 * s, 0.03, 1.51), (0.06 * s, -0.03, 1.475), (0.034 * s, -0.09, 1.425), (0.012 + 0.006 * s, -0.125, 1.39)]
        bb = [(0.085 * s, 0.03, 1.49), (0.09 * s, -0.04, 1.455), (0.058 * s, -0.102, 1.41), (0.014 + 0.016 * s, -0.132, 1.378)]
        p.plate(a, bb, wch, esp=0.0035)
    # nó
    p.loft([lib.st((0.012, -0.133, 1.408), 0.021, 0.012, w=wch), lib.st((0.012, -0.137, 1.385), 0.0235, 0.0145, w=wch),
            lib.st((0.013, -0.139, 1.362), 0.0155, 0.0105, w=wtie)], n=16, sub=4)
    # lâmina caindo torta, afastada do peito
    pts = [(0.013, -0.141, 1.364), (0.016, -0.145, 1.30), (0.021, -0.146, 1.22), (0.026, -0.141, 1.14), (0.03, -0.137, 1.08), (0.033, -0.135, 1.035)]
    larg = [0.016, 0.024, 0.029, 0.033, 0.036, 0.036]
    p.plate([(x - w, y, z) for (x, y, z), w in zip(pts, larg)], [(x + w, y - 0.004, z) for (x, y, z), w in zip(pts, larg)],
            [{"chest": 0.5, "tie": 0.5}, wtie, wtie, wtie, wtie, wtie], esp=0.0032)
    # ponta triangular
    x, y, z = pts[-1]
    p.plate([(x - 0.036, y, z), (x, y, z - 0.045)], [(x + 0.036, y - 0.004, z), (x + 0.001, y - 0.002, z - 0.045)], wtie, esp=0.0032)
    # tira de trás, mais curta
    pts2 = [(-0.004, -0.132, 1.36), (0.0, -0.133, 1.25), (0.004, -0.13, 1.16)]
    p.plate([(q[0] - 0.012, q[1], q[2]) for q in pts2], [(q[0] + 0.012, q[1], q[2]) for q in pts2], wtie, esp=0.0025)


def calca(p, b):
    wh = {"hips": 1}
    p.loft([lib.st((0, 0.003, 0.84), 0.13, 0.095, w=wh, e=2.2), lib.st((0, 0.0, 0.92), 0.158, 0.102, 0.118, w=wh, e=2.3),
            lib.st((0, 0.0, 1.0), 0.156, 0.105, 0.115, w=wh, e=2.3), lib.st((0, 0.002, 1.058), 0.152, 0.106, 0.111, w=wh, e=2.3)], n=40, sub=4)

    def perna(s, n):
        Th, K = b.pos[f"thigh.{n}"]
        A = b.pos[f"shin.{n}"][1]
        th, sh = {f"thigh.{n}": 1}, {f"shin.{n}": 1}
        p.loft([lib.st(Th + Vector((0.004 * s, 0, 0.03)), 0.078, 0.095, w={"hips": 0.5, f"thigh.{n}": 0.5}, side=(1, 0, 0)),
                lib.st(Th + (K - Th) * 0.15, 0.078, 0.09, w=th, side=(1, 0, 0)),
                lib.st(Th + (K - Th) * 0.55, 0.066, 0.074, w=th, side=(1, 0, 0)),
                lib.st(K, 0.0565, 0.06, w={f"thigh.{n}": 0.5, f"shin.{n}": 0.5}, side=(1, 0, 0)),
                lib.st(K + (A - K) * 0.45, 0.052, 0.056, w=sh, side=(1, 0, 0)),
                lib.st(K + (A - K) * 0.84, 0.054, 0.062, w=sh, side=(1, 0, 0)),
                lib.st(A + Vector((0, -0.006, 0.0)), 0.058, 0.073, w={f"shin.{n}": 0.7, f"foot.{n}": 0.3}, side=(1, 0, 0))], n=30, sub=5)

    lados(perna)


def sapatos(p, sola):
    def um(s, n):
        x = 0.095 * s
        ft, to, sh = {f"foot.{n}": 1}, {f"toe.{n}": 1}, {f"shin.{n}": 1}
        # linha do pé: do calcanhar à ponta; f = para cima, b = para baixo
        p.loft([lib.st((x, 0.072, 0.056), 0.012, 0.018, 0.02, w=ft, e=2.4),
                lib.st((x, 0.055, 0.066), 0.0375, 0.034, 0.031, w=ft, e=2.6),
                lib.st((x, 0.01, 0.078), 0.041, 0.056, 0.042, w=ft, e=2.6),
                lib.st((x, -0.05, 0.068), 0.0435, 0.043, 0.032, w=ft, e=2.8),
                lib.st((x, -0.11, 0.05), 0.0475, 0.027, 0.018, w={f"foot.{n}": 0.6, f"toe.{n}": 0.4}, e=2.9),
                lib.st((x, -0.165, 0.044), 0.044, 0.0205, 0.012, w=to, e=2.9),
                lib.st((x, -0.205, 0.04), 0.03, 0.0135, 0.008, w=to, e=2.6),
                lib.st((x, -0.222, 0.037), 0.0095, 0.006, 0.004, w=to)], n=28, sub=5, side=(1, 0, 0))
        # cano por dentro da calça
        p.loft([lib.st((x, 0.012, 0.075), 0.039, 0.044, w=ft, side=(1, 0, 0)), lib.st((x, 0.012, 0.13), 0.038, 0.043, w=sh, side=(1, 0, 0))], n=16, sub=3)
        # sola e salto
        sola.loft([lib.st((x, 0.076, 0.012), 0.02, 0.014, w=ft, e=3), lib.st((x, 0.058, 0.014), 0.039, 0.016, w=ft, e=3),
                   lib.st((x, 0.0, 0.015), 0.04, 0.017, w=ft, e=3)], n=20, sub=3, side=(1, 0, 0))
        sola.loft([lib.st((x, 0.006, 0.0285), 0.036, 0.004, w=ft, e=3), lib.st((x, -0.11, 0.0235), 0.052, 0.005, w={f"foot.{n}": 0.6, f"toe.{n}": 0.4}, e=3),
                   lib.st((x, -0.205, 0.0235), 0.034, 0.005, w=to, e=3), lib.st((x, -0.232, 0.0235), 0.008, 0.004, w=to)], n=20, sub=4, side=(1, 0, 0))

    lados(um)


def cinto(p, m):
    wh = {"hips": 1}
    p.loft([lib.st((0, 0.002, 1.018), 0.163, 0.113, 0.118, w=wh, e=2.3), lib.st((0, 0.002, 1.058), 0.161, 0.113, 0.116, w=wh, e=2.3)], n=44, sub=2, cap=False)
    m.loft([lib.st((0.005, -0.1185, 1.02), 0.024, 0.0045, w=wh, e=5, side=(1, 0, 0)), lib.st((0.005, -0.1185, 1.058), 0.024, 0.0045, w=wh, e=5, side=(1, 0, 0))], n=16, sub=2)


def dobras_camisa(ob):
    r = random.Random(4)
    L = []
    # puxões do peito em direção à cintura, como na ilustração
    for x, top, bot, w, amp in ((-0.13, 1.40, 1.10, 0.024, 0.009), (-0.075, 1.33, 1.08, 0.022, 0.0085), (0.11, 1.36, 1.10, 0.024, 0.0085),
                                (0.15, 1.27, 1.06, 0.022, 0.008), (0.055, 1.25, 1.06, 0.018, 0.007), (-0.15, 1.25, 1.06, 0.02, 0.0075)):
        L.append(((x, -0.13, top), (x * 0.45, -0.13, bot), w, amp))
        L.append(((x, 0.13, top), (x * 0.5, 0.13, bot), w, amp * 0.8))
        L.append(((math.copysign(0.18, x), x * 0.4, top), (math.copysign(0.14, x), x * 0.25, bot), w, amp))
    # bolsão sobre o cinto
    for i in range(12):
        z = r.uniform(1.06, 1.12)
        x = r.uniform(-0.16, 0.16)
        L.append(((x - 0.06, -0.125, z + r.uniform(-0.02, 0.02)), (x + 0.06, -0.125, z + r.uniform(-0.02, 0.02)), 0.014, 0.0075))
        L.append(((x - 0.06, 0.125, z), (x + 0.06, 0.125, z + r.uniform(-0.02, 0.02)), 0.014, 0.0065))
    for s in (-1, 1):
        L.append(((0.17 * s, -0.08, 1.12), (0.17 * s, 0.08, 1.08), 0.016, 0.008))
    # mangas: rugas em volta do cotovelo (posição de descanso, braço aberto a 30 graus)
    for s in (-1, 1):
        S = Vector((0.185 * s, 0.005, 1.455))
        E = S + Vector((0.29 * s, 0.0, -0.957)).normalized() * 0.30
        d = (E - S).normalized()
        n1, n2 = Vector((0, 1, 0)), d.cross(Vector((0, 1, 0))).normalized()
        for i in range(14):
            o = r.uniform(-0.17, 0.06)
            ang = r.uniform(0, 6.28)
            c = E + d * o
            u = n1 * math.cos(ang) + n2 * math.sin(ang)
            v = n1 * -math.sin(ang) + n2 * math.cos(ang)
            q0 = c + u * 0.06 - v * 0.05
            q1 = c + u * 0.06 + v * 0.05 + d * r.uniform(-0.03, 0.03)
            L.append((tuple(q0), tuple(q1), 0.013, 0.0075))
        L.append((tuple(S + d * 0.02 + Vector((0, -0.07, 0.0))), tuple(S + d * 0.14 + Vector((0, -0.06, 0))), 0.016, 0.007))
    lib.folds(ob, L, seed=3, ruido=0.0028, ruido_esc=26)


def dobras_calca(ob):
    r = random.Random(9)
    L = []
    for s in (-1, 1):
        x = 0.09 * s
        # virilha e parte de trás do joelho
        L.append(((0.03 * s, -0.09, 0.86), (0.14 * s, -0.08, 0.95), 0.016, 0.007))
        L.append(((0.05 * s, -0.08, 0.80), (0.15 * s, -0.06, 0.88), 0.016, 0.0055))
        for i in range(7):
            z = r.uniform(0.47, 0.60)
            L.append(((x - 0.05, -0.08, z), (x + 0.05, -0.07, z + r.uniform(-0.04, 0.04)), 0.012, 0.0055))
            L.append(((x - 0.05, 0.07, z), (x + 0.05, 0.06, z + r.uniform(-0.04, 0.04)), 0.012, 0.0055))
        # acúmulo sobre o sapato
        for i in range(6):
            z = r.uniform(0.11, 0.2)
            ang = r.uniform(0, 6.28)
            L.append(((x + 0.05 * math.cos(ang), 0.06 * math.sin(ang), z), (x + 0.05 * math.cos(ang + 1.2), 0.06 * math.sin(ang + 1.2), z + 0.02), 0.012, 0.007))
        # vinco da frente
        L.append(((x, -0.095, 0.82), (x + 0.003 * s, -0.082, 0.18), 0.006, 0.003))
    lib.folds(ob, L, seed=5, ruido=0.0016, ruido_esc=14)


def uv_rosto(ob):
    me = ob.data
    uv = me.uv_layers.new(name="rosto")
    co = lib.coords(me)
    for poly in me.polygons:
        for li, vi in zip(poly.loop_indices, poly.vertices):
            x, _y, z = co[vi]
            uv.data[li].uv = (0.5 + x / 0.2, (z - 1.55) / 0.2)


def main():
    saida = sys.argv[sys.argv.index("--") + 1]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    b = esqueleto()
    rig = b.create("Kai")

    m = dict(
        pele=lib.toon("Pele", ["#5a4b49", "#7f6e68", "#a8978b", "#bfae9f"], [0.0, 0.1, 0.3, 0.72], tex=os.path.join(BUILD, "kai_rosto.png")),
        pele_b=lib.toon("PeleCorpo", ["#5a4b49", "#7f6e68", "#a8978b", "#bfae9f"], [0.0, 0.1, 0.3, 0.72]),
        cabelo=lib.toon("Cabelo", ["#120f10", "#221d1e", "#3a3332", "#5a5150"], [0.0, 0.24, 0.55, 0.86], rim="#6c6462"),
        camisa=lib.toon("Camisa", ["#4d4744", "#6f6864", "#99918b", "#b9b1a9"], [0.0, 0.2, 0.42, 0.74], rim="#cfc8c0"),
        calca=lib.toon("Calca", ["#0f0e0f", "#191718", "#272425", "#37332f"], [0.0, 0.2, 0.44, 0.8], rim="#47423f", rim_lim=0.78),
        gravata=lib.toon("Gravata", ["#0e0e12", "#1c1c22", "#2d2d36", "#3e3e4a"], [0.0, 0.2, 0.5, 0.82], rim="#5a5a68"),
        sapato=lib.toon("Sapato", ["#0d0c0d", "#1b1a1b", "#332f30", "#7f7876"], [0.0, 0.22, 0.6, 0.93], rim="#6a6462"),
        sola=lib.toon("Sola", ["#090808", "#141213"], [0.0, 0.4]),
        metal=lib.toon("Metal", ["#3f3e40", "#6f6e70", "#b4b2b0"], [0.0, 0.3, 0.7]),
    )

    pcab = lib.Prim(b)
    cabeca(pcab)
    ohead = pcab.build("Cabeca", voxel=0.0032, smooth=4, material=m["pele"])
    uv_rosto(ohead)

    pm = lib.Prim(b)
    maos(pm, b)
    omaos = pm.build("Bracos", voxel=0.0026, smooth=3, material=m["pele_b"])

    ph = lib.Prim(b)
    cabelo(ph)
    ohair = ph.build("Cabelo", material=m["cabelo"], smooth=0)

    pc = lib.Prim(b)
    camisa(pc, b)
    oshirt = pc.build("Camisa", voxel=0.0055, smooth=3, material=m["camisa"], cut=decote)
    dobras_camisa(oshirt)

    pcol = lib.Prim(b)
    colarinho(pcol)
    ocol = pcol.build("Colarinho", voxel=0.0028, smooth=2, material=m["camisa"])

    pg = lib.Prim(b)
    gravata(pg)
    otie = pg.build("Gravata", voxel=0.0028, smooth=2, material=m["gravata"])

    pt = lib.Prim(b)
    calca(pt, b)
    opants = pt.build("Calca", voxel=0.006, smooth=3, material=m["calca"])
    dobras_calca(opants)

    ps, psola = lib.Prim(b), lib.Prim(b)
    sapatos(ps, psola)
    oshoe = ps.build("Sapatos", voxel=0.003, smooth=3, material=m["sapato"])
    osola = psola.build("Solas", voxel=0.003, smooth=1, material=m["sola"])

    pb, pmet = lib.Prim(b), lib.Prim(b)
    cinto(pb, pmet)
    obelt = pb.build("Cinto", material=m["sola"], smooth=0)
    obk = pmet.build("Fivela", material=m["metal"], smooth=0)

    for ob, esp in ((ohead, 0.0028), (omaos, 0.0028), (ohair, 0.0013), (oshirt, 0.0042), (ocol, 0.0034), (otie, 0.0034),
                    (opants, 0.0042), (oshoe, 0.0036), (osola, 0.003), (obelt, 0.0026), (obk, 0.0015)):
        lib.bind(ob, rig, b)
        lib.outline(ob, esp)

    bpy.ops.wm.save_as_mainfile(filepath=saida)
    print("OK", {o.name: len(o.data.vertices) for o in bpy.data.objects if o.type == "MESH"})


main()
