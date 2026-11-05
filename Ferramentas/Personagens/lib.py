"""Biblioteca de construção dos personagens (Blender 5.x, só bpy/numpy, sem VRoid).

Fluxo de cada peça: primitivas com pesos por osso -> remesh por voxel (peça contínua)
-> pesos transferidos da primitiva mais próxima -> dobras por deslocamento -> casco de contorno.
Eixos: frente do personagem = -Y, esquerda dele = +X, chão em Z = 0, unidades em metros.
"""
import math

import bmesh
import bpy
import numpy as np
from mathutils import Vector, kdtree


# ------------------------------------------------------------------ esqueleto
class Bones:
    def __init__(self, spec):
        """spec: lista de (nome, cabeça, cauda, pai, conecta?)."""
        self.spec = spec
        self.names = [s[0] for s in spec]
        self.index = {n: i for i, n in enumerate(self.names)}
        self.pos = {s[0]: (Vector(s[1]), Vector(s[2])) for s in spec}

    def create(self, name="Rig"):
        arm = bpy.data.armatures.new(name)
        ob = bpy.data.objects.new(name, arm)
        bpy.context.collection.objects.link(ob)
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.mode_set(mode="EDIT")
        for n, h, t, _p, _c in self.spec:
            b = arm.edit_bones.new(n)
            b.head, b.tail = h, t
            b.roll = 0.0
        for n, _h, _t, p, c in self.spec:
            if p:
                arm.edit_bones[n].parent = arm.edit_bones[p]
                arm.edit_bones[n].use_connect = c
        bpy.ops.object.mode_set(mode="OBJECT")
        return ob


# ------------------------------------------------------------------ primitivas
class Prim:
    """Acumula vértices/faces com um vetor de pesos por vértice (um valor por osso)."""

    def __init__(self, bones):
        self.bones = bones
        self.v, self.f, self.w = [], [], []

    def _weight(self, spec):
        w = np.zeros(len(self.bones.names))
        for name, val in spec.items():
            w[self.bones.index[name]] = val
        return w

    def add(self, verts, faces, weights):
        base = len(self.v)
        self.v.extend(verts)
        self.f.extend([tuple(i + base for i in fc) for fc in faces])
        self.w.extend(weights)

    def loft(self, stations, n=28, sub=6, side=(1, 0, 0), cap=True):
        """Tubo por estações: dict p=centro, a=meia-largura, f=raio à frente, b=raio atrás,
        e=expoente (2 elipse, >2 mais quadrado), w={osso: peso}, side=vetor lateral opcional."""
        st = _densify(stations, sub)
        verts, weights, rings = [], [], len(st)
        pts = [s["p"] for s in st]
        for i, s in enumerate(st):
            t = (pts[min(i + 1, rings - 1)] - pts[max(i - 1, 0)]).normalized()
            u = Vector(s.get("side", side)).normalized()
            u = (u - t * u.dot(t)).normalized()
            v = t.cross(u).normalized()
            e = s.get("e", 2.0)
            w = self._weight_dense(s["w"])
            for k in range(n):
                ang = 2 * math.pi * k / n
                ca, sa = math.cos(ang), math.sin(ang)
                x = s["a"] * math.copysign(abs(ca) ** (2 / e), ca)
                r = s["f"] if sa * s.get("flip", 1) > 0 else s["b"]
                y = r * math.copysign(abs(sa) ** (2 / e), sa)
                verts.append(s["p"] + u * x + v * y)
                weights.append(w)
        faces = []
        for i in range(rings - 1):
            for k in range(n):
                a, b = i * n + k, i * n + (k + 1) % n
                faces.append((a, b, b + n, a + n))
        if cap:
            c0, c1 = len(verts), len(verts) + 1
            verts += [pts[0], pts[-1]]
            weights += [weights[0], weights[-1]]
            for k in range(n):
                faces.append((c0, (k + 1) % n, k))
                j = (rings - 1) * n
                faces.append((c1, j + k, j + (k + 1) % n))
        self.add(verts, faces, weights)

    def _weight_dense(self, w):
        return w if isinstance(w, np.ndarray) else self._weight(w)

    def ellipsoid(self, c, r, w, seg=20, rings=14, rot=None):
        c = Vector(c)
        verts, faces = [], []
        for i in range(rings + 1):
            th = math.pi * i / rings
            for j in range(seg):
                ph = 2 * math.pi * j / seg
                p = Vector((r[0] * math.sin(th) * math.cos(ph), r[1] * math.sin(th) * math.sin(ph), r[2] * math.cos(th)))
                if rot is not None:
                    p = rot @ p
                verts.append(c + p)
        for i in range(rings):
            for j in range(seg):
                a, b = i * seg + j, i * seg + (j + 1) % seg
                faces.append((a, a + seg, b + seg, b))
        self.add(verts, faces, [self._weight(w)] * len(verts))

    def plate(self, a, b, w, esp=0.002, n=None):
        """Chapa fina entre duas polilinhas (colarinho, gravata, aba de camisa). w: pesos por ponto ou único."""
        m = len(a)
        verts, faces, weights = [], [], []
        for i in range(m):
            wi = w[i] if isinstance(w, list) else w
            base = (Vector(b[i]) - Vector(a[i]))
            t = (Vector(a[min(i + 1, m - 1)]) - Vector(a[max(i - 1, 0)]))
            nrm = t.cross(base).normalized()
            for off in (esp, -esp):
                verts.append(Vector(a[i]) + nrm * off)
                verts.append(Vector(b[i]) + nrm * off)
                weights += [self._weight_dense(wi)] * 2
        for i in range(m - 1):
            p, q = i * 4, (i + 1) * 4
            faces += [(p, q, q + 1, p + 1), (p + 3, q + 3, q + 2, p + 2),
                      (p + 1, q + 1, q + 3, p + 3), (p + 2, q + 2, q, p)]
        faces += [(0, 1, 3, 2), ((m - 1) * 4 + 2, (m - 1) * 4 + 3, (m - 1) * 4 + 1, (m - 1) * 4)]
        self.add(verts, faces, weights)

    def build(self, name, voxel=None, smooth=2, material=None, cut=None):
        me = bpy.data.meshes.new(name)
        me.from_pydata([tuple(v) for v in self.v], [], self.f)
        me.update()
        ob = bpy.data.objects.new(name, me)
        bpy.context.collection.objects.link(ob)
        src_co = np.array([tuple(v) for v in self.v])
        src_w = np.array(self.w)
        if voxel:
            mod = ob.modifiers.new("v", "REMESH")
            mod.mode = "VOXEL"
            mod.voxel_size = voxel
            mod.adaptivity = 0.0
            dg = bpy.context.evaluated_depsgraph_get()
            me2 = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
            ob.modifiers.clear()
            ob.data = me2
            bpy.data.meshes.remove(me)
            me = me2
        if smooth:
            bm = bmesh.new()
            bm.from_mesh(me)
            for _ in range(smooth):
                bmesh.ops.smooth_vert(bm, verts=bm.verts, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
            bm.to_mesh(me)
            bm.free()
        if cut:
            bm = bmesh.new()
            bm.from_mesh(me)
            fora = [v for v in bm.verts if cut(v.co)]
            bmesh.ops.delete(bm, geom=fora, context="VERTS")
            bm.to_mesh(me)
            bm.free()
        me.shade_smooth()
        w = transfer_weights(me, src_co, src_w) if voxel else src_w
        ob["_w"] = 0
        ob_weights[ob.name] = w
        if material:
            me.materials.append(material)
        return ob


ob_weights = {}


def _densify(stations, sub):
    """Catmull-Rom nos centros, interpolação suave nos raios e pesos."""
    out = []
    m = len(stations)
    for i in range(m - 1):
        p0 = stations[max(i - 1, 0)]["p"]
        p1, p2 = stations[i]["p"], stations[i + 1]["p"]
        p3 = stations[min(i + 2, m - 1)]["p"]
        for k in range(sub):
            t = k / sub
            t2, t3 = t * t, t * t * t
            p = 0.5 * ((2 * p1) + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3)
            s = {"p": p}
            sm = t * t * (3 - 2 * t)
            for key in ("a", "f", "b", "e"):
                a0 = stations[i].get(key, 2.0)
                a1 = stations[i + 1].get(key, 2.0)
                s[key] = a0 + (a1 - a0) * (0.5 * t + 0.5 * sm)
            s["w"] = _mixw(stations[i]["w"], stations[i + 1]["w"], sm)
            if "side" in stations[i]:
                s["side"] = stations[i]["side"]
            out.append(s)
    last = dict(stations[-1])
    out.append(last)
    return out


def _mixw(a, b, t):
    keys = set(a) | set(b)
    return {k: a.get(k, 0.0) * (1 - t) + b.get(k, 0.0) * t for k in keys}


def st(p, a, f=None, b=None, w=None, e=2.0, **kw):
    """Atalho para uma estação de loft."""
    f = a if f is None else f
    b = f if b is None else b
    d = dict(p=Vector(p), a=a, f=f, b=b, e=e, w=w or {})
    d.update(kw)
    return d


# ------------------------------------------------------------------ pesos
def adjacency(me):
    e = np.zeros(len(me.edges) * 2, dtype=np.int32)
    me.edges.foreach_get("vertices", e)
    return e.reshape(-1, 2)


def coords(me):
    c = np.zeros(len(me.vertices) * 3)
    me.vertices.foreach_get("co", c)
    return c.reshape(-1, 3)


def set_coords(me, c):
    me.vertices.foreach_set("co", c.ravel())
    me.update()


def laplace(values, edges, n, iters, keep=0.5):
    for _ in range(iters):
        s = np.zeros_like(values)
        c = np.zeros(n)
        np.add.at(s, edges[:, 0], values[edges[:, 1]])
        np.add.at(s, edges[:, 1], values[edges[:, 0]])
        np.add.at(c, edges[:, 0], 1)
        np.add.at(c, edges[:, 1], 1)
        c[c == 0] = 1
        shape = (-1,) + (1,) * (values.ndim - 1)
        values = values * keep + s / c.reshape(shape) * (1 - keep)
    return values


def transfer_weights(me, src_co, src_w, smooth_iters=10):
    tree = kdtree.KDTree(len(src_co))
    for i, co in enumerate(src_co):
        tree.insert(co, i)
    tree.balance()
    co = coords(me)
    idx = np.array([tree.find(p)[1] for p in co])
    w = src_w[idx]
    edges = adjacency(me)
    return laplace(w, edges, len(co), smooth_iters)


def bind(ob, rig, bones):
    w = ob_weights[ob.name]
    n = len(ob.data.vertices)
    for b, name in enumerate(bones.names):
        col = w[:, b]
        ids = np.nonzero(col > 0.005)[0]
        if len(ids) == 0:
            continue
        vg = ob.vertex_groups.new(name=name)
        for i in ids:
            vg.add([int(i)], float(col[i]), "REPLACE")
    ob.parent = rig
    m = ob.modifiers.new("Rig", "ARMATURE")
    m.object = rig
    return n


# ------------------------------------------------------------------ dobras de tecido
def folds(ob, lista, seed=0, ruido=0.0, ruido_esc=18.0):
    """Desloca a malha na normal. lista: (p0, p1, largura, amplitude): crista ao longo do segmento.
    A crista é assimétrica (um lado cai rápido), o que dá a borda dura de sombra da ilustração."""
    me = ob.data
    co = coords(me)
    no = np.zeros(len(co) * 3)
    me.vertex_normals.foreach_get("vector", no)
    no = no.reshape(-1, 3)
    d = np.zeros(len(co))
    for p0, p1, w, amp in lista:
        a, b = np.array(p0, float), np.array(p1, float)
        ab = b - a
        t = np.clip(((co - a) @ ab) / max(ab @ ab, 1e-9), 0, 1)
        near = a + t[:, None] * ab
        delta = co - near
        dist = np.sqrt((delta * delta).sum(1))
        # lado da crista: direção perpendicular ao segmento e à normal
        side = np.cross(no, ab / (np.linalg.norm(ab) + 1e-9))
        s = (delta * side).sum(1)
        k = np.where(s > 0, w * 0.45, w)
        taper = np.sin(np.pi * np.clip(t, 0, 1)) ** 0.5
        d += amp * np.exp(-((dist / k) ** 2)) * taper
    if ruido:
        rng = np.random.default_rng(seed)
        for _ in range(7):
            f = rng.normal(0, ruido_esc, 3)
            d += ruido * np.sin(co @ f + rng.uniform(0, 6.28)) / 3
    set_coords(me, co + no * d[:, None])
    return ob


# ------------------------------------------------------------------ cabelo
class Skull:
    """Elipsoide onde as mechas nascem e correm."""

    def __init__(self, c, r):
        self.c, self.r = Vector(c), Vector(r)

    def at(self, lat, lon, lift=0.0):
        """lat 90 = topo, 0 = linha do equador; lon 0 = frente (-Y), 90 = esquerda dele (+X)."""
        la, lo = math.radians(lat), math.radians(lon)
        d = Vector((math.cos(la) * math.sin(lo), -math.cos(la) * math.cos(lo), math.sin(la)))
        return self.c + Vector((d.x * self.r.x, d.y * self.r.y, d.z * self.r.z)) * (1 + lift)


def lock(prim, skull, a, b, width, w, lift=0.03, belly=0.05, flick=0.0, hang=0.0, sway=0.0, thick=0.42, seg=14, n=8):
    """Mecha: de a=(lat, lon) a b=(lat, lon) sobre o crânio. hang = metros caindo reto além de b;
    flick = pontas para fora; sway = desvio lateral em S. Seção lenticular, afina até a ponta."""
    pts = []
    for i in range(seg + 1):
        t = i / seg
        la = a[0] + (b[0] - a[0]) * t
        lo = a[1] + (b[1] - a[1]) * t
        r = lift + belly * math.sin(math.pi * t) + flick * t ** 3
        p = skull.at(la, lo, r)
        p.z -= hang * t ** 2
        p.x += sway * math.sin(t * math.pi * 1.5)
        pts.append(p)
    stations = []
    for i, p in enumerate(pts):
        t = i / seg
        wd = width * (min(1.0, 0.45 + t * 3.2)) * (1 - t ** 2.2) + 0.0012
        radial = (p - skull.c).normalized()
        stations.append(dict(p=p, a=wd, f=wd * thick, b=wd * thick, e=2.0, w=w,
                             side=_side_of(pts, i, radial)))
    prim.loft(stations, n=n, sub=1)


def _side_of(pts, i, radial):
    t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
    return tuple(t.cross(radial).normalized())


# ------------------------------------------------------------------ materiais toon
def toon(name, cores, stops, tex=None, rim=None, rim_lim=0.62):
    """cores: da sombra para a luz; stops: posição de cada cor no ramp (constante)."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse")
    dif.inputs["Color"].default_value = (1, 1, 1, 1)
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "CONSTANT"
    cr = ramp.color_ramp
    while len(cr.elements) < len(cores):
        cr.elements.new(0.99)
    for el, c, s in zip(cr.elements, cores, stops):
        el.position = s
        el.color = (*srgb(c), 1)
    nt.links.new(dif.outputs[0], s2r.inputs[0])
    nt.links.new(s2r.outputs[0], ramp.inputs[0])
    color = ramp.outputs[0]
    if rim:
        lw = nt.nodes.new("ShaderNodeLayerWeight")
        lw.inputs[0].default_value = 0.5
        gt = nt.nodes.new("ShaderNodeMath")
        gt.operation = "GREATER_THAN"
        gt.inputs[1].default_value = rim_lim
        nt.links.new(lw.outputs["Facing"], gt.inputs[0])
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        dot = nt.nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        dot.inputs[1].default_value = (0.75, 0.35, 0.55)  # recorte vindo de trás e de cima
        nt.links.new(geo.outputs["Normal"], dot.inputs[0])
        gt2 = nt.nodes.new("ShaderNodeMath")
        gt2.operation = "GREATER_THAN"
        gt2.inputs[1].default_value = 0.15
        nt.links.new(dot.outputs["Value"], gt2.inputs[0])
        mul = nt.nodes.new("ShaderNodeMath")
        mul.operation = "MULTIPLY"
        nt.links.new(gt.outputs[0], mul.inputs[0])
        nt.links.new(gt2.outputs[0], mul.inputs[1])
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        nt.links.new(mul.outputs[0], mix.inputs["Factor"] if "Factor" in mix.inputs else mix.inputs[0])
        nt.links.new(color, mix.inputs["A"] if False else mix.inputs[6])
        mix.inputs[7].default_value = (*srgb(rim), 1)
        color = mix.outputs[2]
    if tex:
        img = bpy.data.images.load(tex, check_existing=True)
        img.colorspace_settings.name = "sRGB"
        ti = nt.nodes.new("ShaderNodeTexImage")
        ti.image = img
        ti.extension = "CLIP"
        uv = nt.nodes.new("ShaderNodeUVMap")
        uv.uv_map = "rosto"
        nt.links.new(uv.outputs[0], ti.inputs[0])
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        nt.links.new(ti.outputs["Alpha"], mix.inputs[0])
        nt.links.new(color, mix.inputs[6])
        nt.links.new(ti.outputs["Color"], mix.inputs[7])
        color = mix.outputs[2]
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Strength"].default_value = 1.0
    nt.links.new(color, em.inputs[0])
    nt.links.new(em.outputs[0], out.inputs[0])
    return m


def srgb(hexa):
    h = hexa.lstrip("#")
    return tuple(_lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4))


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def line_material(cor="#141112"):
    m = bpy.data.materials.get("Linha")
    if m:
        return m
    m = bpy.data.materials.new("Linha")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*srgb(cor), 1)
    nt.links.new(em.outputs[0], out.inputs[0])
    m.use_backface_culling = True
    m.use_backface_culling_shadow = True
    return m


def outline(ob, espessura):
    """Casco invertido: a linha acompanha silhueta e bordas entre peças em qualquer pose."""
    ob.data.materials.append(line_material())
    s = ob.modifiers.new("Linha", "SOLIDIFY")
    s.thickness = espessura
    s.offset = 1.0
    s.use_flip_normals = True
    s.use_rim = False
    s.material_offset = len(ob.data.materials) - 1
    return s
