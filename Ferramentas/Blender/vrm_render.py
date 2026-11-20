"""Renderiza um personagem do VRoid (.vrm) em frames PNG laterais com look toon.

blender --background --python vrm_render.py -- <kai|souta|ren> <arquivo.vrm> <saida_dir> [clip ...]
          [--preview]   (renderiza so 1 frame de cada clip, em outra pasta)
O personagem olha para +X na imagem (direita). Camera ortografica 256x384.
"""
import math
import os
import shutil
import sys
import tempfile

import json

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Matrix, Vector

import retarget

CLIPS = {"idle": 8, "walk": 8, "jump": 1, "fall": 1, "talk": 8, "act": 10}

# personalidade (graus). lean = inclinacao do tronco, head = cabeca a frente
STYLE = {
    "kai": dict(lean=15, head=4, stride=28, arm=22, elbow=20, bob=0.024, sh=7, knee=8),
    "souta": dict(lean=-2, head=-4, stride=32, arm=24, elbow=16, bob=0.026, sh=1, knee=3),
    "ren": dict(lean=14, head=6, stride=21, arm=9, elbow=44, bob=0.016, sh=9, knee=12),
}


# ------------------------------------------------------------------ cena
def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    RES = int(os.environ.get("RES", "1"))
    sc.render.resolution_x, sc.render.resolution_y = 256 * RES, 384 * RES
    sc.view_settings.view_transform = "Standard"
    ld = bpy.data.lights.new("sun", "SUN")
    ld.energy = 2.0
    lo = bpy.data.objects.new("sun", ld)
    bpy.context.collection.objects.link(lo)
    lo.rotation_mode = "QUATERNION"
    lo.rotation_quaternion = Vector((0, 0, -1)).rotation_difference(Vector((0.55, 0.55, -0.9)))
    cd = bpy.data.cameras.new("cam")
    cd.type = "ORTHO"
    cd.ortho_scale = 2.3
    co = bpy.data.objects.new("cam", cd)
    bpy.context.collection.objects.link(co)
    co.location = (-6, 0, 1.1)
    co.rotation_euler = (math.radians(90), 0, math.radians(-90))   # olha para +X
    sc.camera = co
    return sc


# ------------------------------------------------------------------ material toon
OBJ_DIMS = (1.0, 1.0, 1.0)   # dimensoes (m) da malha sendo processada


def toon_material(src, outline=False, flat=None):
    mat = bpy.data.materials.new(src.name + ("_ol" if outline else "_toon"))
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    if outline:
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs["Color"].default_value = (0.03, 0.025, 0.04, 1)
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        mat.use_backface_culling = True
        return mat
    # cor base + alpha da textura original
    tex, base, alpha_from_tex = None, (0.8, 0.8, 0.8, 1), False
    if src.use_nodes:
        for n in src.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image:
                tex = n.image
                break
        for n in src.node_tree.nodes:
            if n.type == "BSDF_PRINCIPLED":
                base = tuple(n.inputs["Base Color"].default_value)
                alpha_from_tex = n.inputs["Alpha"].is_linked
                break
    hsv = mul = None
    if flat is not None and flat[0] == "hsv":     # ajusta tom/saturacao/brilho mantendo a textura
        hsv = flat[1:]
    elif flat is not None and flat[0] == "mul":   # multiplica a textura por uma cor
        mul = flat[1:]
    elif flat is not None:                        # cor chapada (ignora a textura)
        tex, base, alpha_from_tex = None, (*flat, 1), False
    col = nt.nodes.new("ShaderNodeTexImage") if tex else nt.nodes.new("ShaderNodeRGB")
    if tex:
        col.image = tex
        col.interpolation = "Linear"
        color_out = col.outputs["Color"]
        if hsv:
            hn = nt.nodes.new("ShaderNodeHueSaturation")
            hn.inputs["Hue"].default_value = hsv[0]
            hn.inputs["Saturation"].default_value = hsv[1]
            hn.inputs["Value"].default_value = hsv[2]
            nt.links.new(color_out, hn.inputs["Color"])
            color_out = hn.outputs["Color"]
        if mul:
            mn = nt.nodes.new("ShaderNodeMix")
            mn.data_type = "RGBA"
            mn.blend_type = "MULTIPLY"
            mn.inputs[7].default_value = (*mul, 1)
            # so tinge o que e escuro/medio (cardiga); o branco da camisa fica como esta
            bw = nt.nodes.new("ShaderNodeRGBToBW")
            mask = nt.nodes.new("ShaderNodeValToRGB")
            mask.color_ramp.elements[0].position = 0.45
            mask.color_ramp.elements[0].color = (1, 1, 1, 1)
            mask.color_ramp.elements[1].position = 0.65
            mask.color_ramp.elements[1].color = (0, 0, 0, 1)
            nt.links.new(color_out, bw.inputs["Color"])
            nt.links.new(bw.outputs["Val"], mask.inputs["Fac"])
            nt.links.new(mask.outputs["Color"], mn.inputs[0])
            nt.links.new(color_out, mn.inputs[6])
            color_out = mn.outputs[2]
    else:
        col.outputs[0].default_value = base
        color_out = col.outputs[0]
    # ---- "envelhecimento": mais escuro e frio, sujo perto dos pes e manchas (olhos ficam como estao)
    if "_EYE" not in src.name and not os.environ.get("NODIRT"):
        tc = nt.nodes.new("ShaderNodeTexCoord")          # Generated: preso a malha, nao "nada" na animacao
        tint = nt.nodes.new("ShaderNodeMix")
        tint.data_type, tint.blend_type = "RGBA", "MULTIPLY"
        tint.inputs[0].default_value = 1.0
        tint.inputs[7].default_value = (0.66, 0.67, 0.76, 1)
        nt.links.new(color_out, tint.inputs[6])
        color_out = tint.outputs[2]
        noise = nt.nodes.new("ShaderNodeTexNoise")
        noise.noise_dimensions = "3D"
        noise.inputs["Scale"].default_value = 5.0           # ~5 ciclos por metro (manchas de ~20 cm)
        noise.inputs["Detail"].default_value = 3.0
        noise.inputs["Roughness"].default_value = 0.55
        # Generated vai de 0 a 1 em cada eixo da malha (larga e fina em T-pose): multiplicar pelas
        # dimensoes em metros deixa o ruido isotropico (sem listras esticadas)
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = OBJ_DIMS
        nt.links.new(tc.outputs["Generated"], mp.inputs["Vector"])
        nt.links.new(mp.outputs["Vector"], noise.inputs["Vector"])
        dirt = nt.nodes.new("ShaderNodeValToRGB")
        skin = "SKIN" in src.name or "FACE" in src.name
        dirt.color_ramp.elements[0].position = 0.35
        dirt.color_ramp.elements[0].color = (0.80, 0.78, 0.80, 1) if skin else (0.52, 0.50, 0.54, 1)
        dirt.color_ramp.elements[1].position = 0.70
        dirt.color_ramp.elements[1].color = (1, 1, 1, 1)
        nt.links.new(noise.outputs["Fac"], dirt.inputs["Fac"])
        stain = nt.nodes.new("ShaderNodeMix")
        stain.data_type, stain.blend_type = "RGBA", "MULTIPLY"
        stain.inputs[0].default_value = 1.0
        nt.links.new(color_out, stain.inputs[6])
        nt.links.new(dirt.outputs["Color"], stain.inputs[7])
        color_out = stain.outputs[2]
        if any(k in src.name for k in ("_Body_", "Bottoms", "Shoes", "Tops", "Accessory")):
            sep = nt.nodes.new("ShaderNodeSeparateXYZ")
            nt.links.new(tc.outputs["Generated"], sep.inputs[0])
            hm = nt.nodes.new("ShaderNodeMapRange")
            hm.clamp = True
            hm.inputs["From Min"].default_value = 0.0
            hm.inputs["From Max"].default_value = 0.65
            hm.inputs["To Min"].default_value = 0.30
            hm.inputs["To Max"].default_value = 1.0
            nt.links.new(sep.outputs["Z"], hm.inputs["Value"])
            low = nt.nodes.new("ShaderNodeMix")
            low.data_type, low.blend_type = "RGBA", "MULTIPLY"
            low.inputs[0].default_value = 1.0
            nt.links.new(color_out, low.inputs[6])
            nt.links.new(hm.outputs["Result"], low.inputs[7])
            color_out = low.outputs[2]
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 1.0
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(diff.outputs[0], s2r.inputs[0])
    nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], mix.inputs[6])
    nt.links.new(color_out, mix.inputs[7])
    nt.links.new(mix.outputs[2], em.inputs["Color"])
    ramp.color_ramp.interpolation = "CONSTANT"
    if os.environ.get("NORAMP"):
        ramp.color_ramp.interpolation = "LINEAR"
    el = ramp.color_ramp.elements
    el[0].position, el[0].color = 0.0, (0.36, 0.38, 0.52, 1)
    el[1].position, el[1].color = 0.30, (0.70, 0.71, 0.80, 1)
    e3 = el.new(0.66)
    e3.color = (0.92, 0.92, 0.97, 1)
    if tex and alpha_from_tex:
        tr = nt.nodes.new("ShaderNodeBsdfTransparent")
        mx = nt.nodes.new("ShaderNodeMixShader")
        nt.links.new(col.outputs["Alpha"], mx.inputs[0])
        nt.links.new(tr.outputs[0], mx.inputs[1])
        nt.links.new(em.outputs[0], mx.inputs[2])
        nt.links.new(mx.outputs[0], out.inputs["Surface"])
        mat.surface_render_method = "BLENDED"
    else:
        nt.links.new(em.outputs[0], out.inputs["Surface"])
    return mat


# recolore pecas pelo nome do material do VRoid (cor chapada, linear). Aplicado a todos
# os personagens + ajustes de cada um.
RECOLOR = {
    "*": {"Shoes": (0.025, 0.022, 0.03)},
    "souta": {"Accessory_Tie": (0.03, 0.03, 0.04), "_HAIR": ("hsv", 0.5, 0.85, 0.5)},
    "kai": {"Accessory_Tie": (0.07, 0.08, 0.14)},
    "ren": {"Accessory_Tie": (0.05, 0.05, 0.07), "007_01_Tops": (0.14, 0.30, 0.46)},   # cardiga azul bebe (camada 01; a camisa e a 02)
}


def flat_for(name, mat_name):
    for table in (RECOLOR["*"], RECOLOR.get(name, {})):
        for key, rgb in table.items():
            if key in mat_name:
                return rgb
    return None


def _bar(p0, p1, r, mat, arm, inv):
    """Cilindro fino entre dois pontos (espaco do mundo), preso ao osso da cabeca."""
    d = Vector(p1) - Vector(p0)
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=d.length, location=(Vector(p0) + Vector(p1)) / 2)
    o = bpy.context.active_object
    o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
    bpy.ops.object.shade_smooth()
    o.data.materials.append(mat)
    o.parent, o.parent_type, o.parent_bone = arm, "BONE", "J_Bip_C_Head"
    o.matrix_parent_inverse = inv
    return o


def add_glasses(arm):
    """Oculos redondos finos presos ao osso da cabeca (modelado na pose de descanso)."""
    mat = toon_material(bpy.data.materials.new("glasses"), flat=(0.03, 0.03, 0.04))
    hb = arm.pose.bones["J_Bip_C_Head"].bone
    # filhos "presos a osso" usam a PONTA (tail) do osso como origem
    inv = (arm.matrix_world @ hb.matrix_local @ Matrix.Translation((0, hb.length, 0))).inverted()
    face = bpy.data.objects["Face"]
    ys = [(face.matrix_world @ v.co).y for v in face.data.vertices]
    yf = min(ys) - 0.006                                   # frente do rosto (olha para -Y)
    iris = [i for i, m in enumerate(face.data.materials) if "EyeIris" in m.name]
    pts = {1: [], -1: []}
    for poly in face.data.polygons:
        if poly.material_index in iris:
            for vi in poly.vertices:
                w = face.matrix_world @ face.data.vertices[vi].co
                pts[1 if w.x > 0 else -1].append(w)
    eyes = []
    for sg in (1, -1):
        c = sum(pts[sg], Vector()) / len(pts[sg])
        eyes.append((c.x * 0.88, arm.data.bones["J_Adj_L_FaceEye"].head_local.z))
    parts = []
    for ex, ez in eyes:
        bpy.ops.mesh.primitive_torus_add(major_radius=0.034, minor_radius=0.0028, major_segments=40,
                                         minor_segments=8, location=(ex, yf, ez),
                                         rotation=(math.radians(90), 0, 0))
        o = bpy.context.active_object
        bpy.ops.object.shade_smooth()
        o.data.materials.append(mat)
        o.parent, o.parent_type, o.parent_bone = arm, "BONE", "J_Bip_C_Head"
        o.matrix_parent_inverse = inv
        parts.append(o)
        sgn = 1 if ex > 0 else -1
        # haste: do canto externo do aro ate a orelha
        parts.append(_bar((ex + sgn * 0.034, yf, ez + 0.0), (sgn * 0.104, yf + 0.075, ez + 0.002),
                          0.0028, mat, arm, inv))
    parts.append(_bar((eyes[0][0] - 0.034, yf, eyes[0][1] + 0.01), (eyes[1][0] + 0.034, yf, eyes[1][1] + 0.01),
                      0.0028, mat, arm, inv))
    return parts


def make_toon(meshes, name):
    global OBJ_DIMS
    for o in meshes:
        OBJ_DIMS = tuple(max(0.05, d) for d in o.dimensions)
        orig = list(o.data.materials)
        n = len(orig)
        for i, m in enumerate(orig):
            o.data.materials[i] = toon_material(m, flat=flat_for(name, m.name))
        if o.name in ("Body", "Hair") and not os.environ.get("NOOUTLINE"):
            for m in orig:
                o.data.materials.append(toon_material(m, outline=True))
            sol = o.modifiers.new("outline", "SOLIDIFY")
            sol.thickness = -0.006
            sol.offset = 1
            sol.use_flip_normals = True
            sol.material_offset = n


# ------------------------------------------------------------------ pose
class Posable:
    """Poses por rotacoes no espaco do MUNDO. Cada chamada de rot() soma uma rotacao ao osso (aplicada
    depois das anteriores, ao redor do eixo do mundo); apply() converte tudo para a rotacao local de cada
    osso, de cima para baixo na hierarquia, para que filhos acompanhem os pais e balancem no eixo certo."""

    def __init__(self, arm):
        self.arm = arm
        self.rest = {b.name: b.matrix_local.to_3x3() for b in arm.data.bones}
        self.ops = {}
        for pb in arm.pose.bones:
            pb.rotation_mode = "QUATERNION"

    def reset(self):
        self.ops = {}
        for pb in self.arm.pose.bones:
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.location = (0, 0, 0)

    def rot(self, bone, axis, deg):
        if bone in self.rest:
            self.ops.setdefault(bone, []).append((axis, deg))

    def apply(self):
        ident = Matrix.Identity(3)
        done = {}

        def visit(b):
            own = ident.copy()
            for axis, deg in self.ops.get(b.name, []):
                own = Matrix.Rotation(math.radians(deg), 3, axis) @ own
            dp = done[b.parent.name] if b.parent else ident
            done[b.name] = own @ dp
            r = self.rest[b.name]
            q = r.inverted() @ dp.inverted() @ done[b.name] @ r
            self.arm.pose.bones[b.name].rotation_quaternion = q.to_quaternion()
            for c in b.children:
                visit(c)

        for b in self.arm.data.bones:
            if b.parent is None:
                visit(b)


    def apply_world(self, D, extra_ops):
        """Aplica rotacoes-mundo absolutas D (osso -> 3x3) vindas do mocap, mais ajustes aditivos extra_ops
        (osso -> [(eixo, graus)]) que se propagam para os filhos."""
        ident = Matrix.Identity(3)
        base_done, extra, tot = {}, {}, {}

        def visit(b):
            e = ident.copy()
            for axis, deg in extra_ops.get(b.name, []):
                e = Matrix.Rotation(math.radians(deg), 3, axis) @ e
            par = b.parent
            extra[b.name] = e @ (extra[par.name] if par else ident)
            base = D.get(b.name)
            if base is None:                       # sem par no Mixamo: acompanha o pai
                base = base_done[par.name] if par else ident
            base_done[b.name] = base
            tot[b.name] = extra[b.name] @ base
            dp = tot[par.name] if par else ident
            r = self.rest[b.name]
            q = r.inverted() @ dp.inverted() @ tot[b.name] @ r
            self.arm.pose.bones[b.name].rotation_quaternion = q.to_quaternion()
            for c in b.children:
                visit(c)

        for b in self.arm.data.bones:
            if b.parent is None:
                visit(b)

    def set_hips_world(self, w):
        pb = self.arm.pose.bones["J_Bip_C_Hips"]
        pb.location = self.rest["J_Bip_C_Hips"].inverted() @ Vector(w)


def find_sides(arm):
    """Retorna (near, far): sufixo L/R do braco mais proximo da camera (x menor)."""
    xl = arm.data.bones["J_Bip_L_UpperArm"].head_local.x
    xr = arm.data.bones["J_Bip_R_UpperArm"].head_local.x
    return ("L", "R") if xl < xr else ("R", "L")


X, Y, Z = "X", "Y", "Z"
# a personagem olha para -Y (mundo). "frente" = girar para -Y ao redor de X: sinal abaixo.
# rot(axis X, +deg) leva +Z para -Y?  Rx(+): y' = -z*sin => +Z vira -Y. ok: +deg = frente para o que sobe.
# para um membro que pende (-Z): +deg leva -Z para +Y (tras). Logo, perna para frente = -deg.


def pose(P, name, near, far, clip, f):
    s = STYLE[name]
    P.reset()
    n = CLIPS[clip]
    ph = 2 * math.pi * f / n
    lean, head = s["lean"], s["head"]
    if clip == "walk":
        lean += 3
    elif clip == "jump":
        lean += 4

    def swing(bone, deg_forward):          # para membros pendentes: frente = -deg em X
        P.rot(bone, X, -deg_forward)

    UA = lambda side: f"J_Bip_{side}_UpperArm"
    LA = lambda side: f"J_Bip_{side}_LowerArm"
    UL = lambda side: f"J_Bip_{side}_UpperLeg"
    LL = lambda side: f"J_Bip_{side}_LowerLeg"
    FT = lambda side: f"J_Bip_{side}_Foot"

    def ua(side, deg):                     # braco: angulo ABSOLUTO em relacao a vertical (compensa o tronco)
        swing(UA(side), deg - lean)

    def la(side, deg):                     # antebraco: dobra do cotovelo
        swing(LA(side), deg)

    # bracos descem da T-pose (giro no eixo Y)
    for side in (near, far):
        xs = P.arm.data.bones[UA(side)].head_local.x
        P.rot(UA(side), Y, 76 if xs > 0 else -76)
    for side in (near, far):                # a inclinacao do quadril nao deve arrastar as pernas
        swing(UL(side), lean * 0.15)

    hips_dz = 0.0
    if clip == "walk":
        A, K = s["stride"], 48
        for side, sgn in ((near, 1), (far, -1)):
            a = A * math.sin(ph) * sgn
            swing(UL(side), a)
            flex = K * max(0.0, math.cos(ph) * sgn) + s["knee"]
            swing(LL(side), -flex)
            swing(FT(side), -a * 0.35 + flex * 0.45)
            ua(side, s["sh"] - s["arm"] * math.sin(ph) * sgn)
            la(side, s["elbow"] + 14 * max(0.0, -math.sin(ph) * sgn))
        hips_dz = -s["bob"] * abs(math.sin(ph))
        swing("J_Bip_C_Head", 2.5 * math.sin(2 * ph))
        swing("J_Bip_C_Chest", 1.5 * math.sin(2 * ph))
    elif clip in ("idle", "talk", "act"):
        b = math.sin(ph)
        hips_dz = -0.004 * (1 - b)
        swing("J_Bip_C_Chest", 1.5 * b)
        swing("J_Bip_C_Head", 1.2 * math.sin(ph + 0.8))
        swing(UL(near), 4)                                    # peso mais num pe so
        swing(LL(near), s["knee"] * 0.6)
        swing(LL(far), s["knee"])
        for side in (near, far):
            ua(side, s["sh"] + 1.2 * b)
            la(side, s["elbow"])
        if name == "souta":          # mao no bolso, ombros para tras, confiante
            ua(near, -14)
            la(near, 62)
        elif name == "ren":          # maos juntas na frente, encolhido, tremendo
            ua(near, 16)
            la(near, 62)
            ua(far, 12)
            la(far, 56)
            swing("J_Bip_C_Chest", 1.2 * math.sin(ph * 3))
        if clip == "talk":
            g = max(0.0, math.sin(ph))
            ua(far, s["sh"] + 30 * g)
            la(far, s["elbow"] + 55 * g)
            swing("J_Bip_C_Head", 4 * math.sin(2 * ph))
        if clip == "act":            # gesto de cada um, sobe e desce (0 -> 1 -> 0)
            k = math.sin(math.pi * f / (n - 1))
            if name == "souta":      # tira a mao do bolso e olha o relogio
                ua(near, -14 + 90 * k)
                la(near, 62 - 14 * k)
                swing("J_Bip_C_Head", 16 * k)
            elif name == "kai":      # suspiro: ergue a cabeca e solta o peito
                swing("J_Bip_C_Head", -14 * k)
                swing("J_Bip_C_Chest", -6 * k)
                hips_dz -= 0.012 * k
            elif name == "ren":      # olha por cima do ombro, assustado
                P.rot("J_Bip_C_Head", Z, -70 * k)
                P.rot("J_Bip_C_Neck", Z, -20 * k)
                swing("J_Bip_C_Chest", -3 * k)
    elif clip == "jump":
        swing(UL(near), 40)
        swing(LL(near), -75)
        swing(UL(far), -12)
        swing(LL(far), -28)
        ua(near, -45)
        la(near, 40)
        ua(far, 35)
        la(far, 45)
    elif clip == "fall":
        swing(UL(near), 20)
        swing(LL(near), -32)
        swing(UL(far), -22)
        swing(LL(far), -16)
        ua(near, -75)
        ua(far, -58)
        for side in (near, far):
            la(side, 25)
    # postura: tronco inclinado (+deg em X inclina para a frente), cabeca compensa
    P.rot("J_Bip_C_Hips", X, lean * 0.15)
    P.rot("J_Bip_C_Spine", X, lean * 0.4)
    P.rot("J_Bip_C_Chest", X, lean * 0.45)
    P.rot("J_Bip_C_Neck", X, head * 0.45)
    P.rot("J_Bip_C_Head", X, head * 0.55 - lean * 0.15)
    P.apply()
    hips = P.arm.pose.bones["J_Bip_C_Hips"]
    hips.location.z = hips_dz
    bpy.context.view_layer.update()


# ------------------------------------------------------------------ mocap (Mixamo)
MIXAMO_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Animacoes", "mixamo")
SOURCES = {
    "kai": {"idle": "Sad Idle", "walk": "Sad Walk", "talk": "Talking (1)", "jump": "Jump", "fall": "Jump"},
    "souta": {"idle": "Standing Idle", "walk": "Walking", "talk": "Talking", "jump": "Jump", "fall": "Jump"},
    "ren": {"idle": "Nervously Look Around", "walk": "Sad Walk", "talk": "Talking (1)", "jump": "Jump", "fall": "Jump"},
}
IDLE_FPS, TALK_FPS, ACT_FPS, LOOP_FPS = 7, 8, 9, 10
JUMP_DAMP = 0.5          # pulo/queda: 50% do mocap + 50% da pose parada (o salto de corrida original e dramatico demais)
WALK_FRAMES, ACT_FRAMES, JUMP_FRAMES, FALL_FRAMES = 16, 30, 8, 8
MOCAP_CLIPS = ["idle", "walk", "talk", "act", "jump", "fall"]

# camada de personalidade (graus, aditiva, rotacao em X: + inclina para a frente), sempre ligada
STATIC = {
    "kai": [("J_Bip_C_Spine", X, -5), ("J_Bip_C_Chest", X, -5), ("J_Bip_C_Neck", X, -5), ("J_Bip_C_Head", X, -4)],
    "souta": [("J_Bip_C_Chest", X, -2), ("J_Bip_C_Head", X, -2)],
    "ren": [("J_Bip_C_Spine", X, -3), ("J_Bip_C_Chest", X, -3), ("J_Bip_C_Neck", X, -2)],
}


def extras_for(name, clip, k, near, far):
    """Ajustes aditivos: personalidade fixa + gesto do clipe act (k sobe 0 -> 1 -> 0)."""
    ops = {}

    def add(bone, axis, deg):
        ops.setdefault(bone, []).append((axis, deg))

    for bone, axis, deg in STATIC.get(name, []):
        add(bone, axis, deg)
    if clip == "act":
        if name == "souta":       # olha o relogio
            add(f"J_Bip_{near}_UpperArm", X, -55 * k)
            add(f"J_Bip_{near}_LowerArm", X, -75 * k)
            add("J_Bip_C_Head", X, 14 * k)
        elif name == "kai":       # suspiro
            add("J_Bip_C_Head", X, -14 * k)
            add("J_Bip_C_Chest", X, -6 * k)
        elif name == "ren":       # olha por cima do ombro
            add("J_Bip_C_Head", Z, -70 * k)
            add("J_Bip_C_Neck", Z, -20 * k)
    return ops


def clip_plan(sources, name, clip):
    """Lista de (fonte, quadro_origem, remover_deslocamento, k) para cada quadro de saida."""
    idle = sources.get("idle")
    if clip in ("idle", "talk"):
        src = sources[clip]
        fps = IDLE_FPS if clip == "idle" else TALK_FPS
        n = max(8, round((src.f1 - src.f0) / 30.0 * fps))
        return [(src, src.f0 + (src.f1 - src.f0) * i / n, False, 0.0, 1.0) for i in range(n)]
    if clip == "walk":
        src = sources["walk"]
        n = WALK_FRAMES
        return [(src, src.f0 + (src.f1 - src.f0) * i / n, True, 0.0, 1.0) for i in range(n)]
    if clip == "act":
        step = 30.0 / ACT_FPS
        span = idle.f1 - idle.f0
        return [(idle, idle.f0 + (i * step) % span, False, math.sin(math.pi * i / (ACT_FRAMES - 1)), 1.0)
                for i in range(ACT_FRAMES)]
    if clip == "jump":      # do agachar ate o apice (Jump.fbx: agacha no quadro ~3, apice no ~11 de 29)
        src = sources["jump"]
        a, b = src.f0 + 2, src.f0 + 10
        return [(src, a + (b - a) * i / (JUMP_FRAMES - 1), False, 0.0, JUMP_DAMP) for i in range(JUMP_FRAMES)]
    if clip == "fall":
        src = sources["fall"]        # descida do Jump.fbx: do apice (~quadro 12) ate preparar o pouso (~20)
        a, b = src.f0 + 11, src.f0 + 19
        return [(src, a + (b - a) * i / (FALL_FRAMES - 1), False, 0.0, JUMP_DAMP) for i in range(FALL_FRAMES)]
    raise ValueError(clip)


def sole_height(arm, rest):
    """Altura (m) do ponto mais baixo dos pes em relacao ao chao (0 = encostado)."""
    low = 1e9
    for S in ("L", "R"):
        foot = arm.pose.bones[f"J_Bip_{S}_Foot"]
        low = min(low, (arm.matrix_world @ foot.head).z - rest[S][0])
        toe = arm.pose.bones.get(f"J_Bip_{S}_ToeBase")
        if toe is not None and rest[S][1] is not None:
            low = min(low, (arm.matrix_world @ toe.tail).z - rest[S][1])
    return low


def render_mocap(sc, arm, P, name, near, far, outdir, clips, preview):
    needed = set()
    for role, f in SOURCES[name].items():
        if role in clips or (role == "idle" and ("act" in clips or "jump" in clips or "fall" in clips)):
            needed.add(f)
    srcs = {}
    for file in needed:
        s_ = retarget.MixamoSource(os.path.join(MIXAMO_DIR, file + ".fbx"))
        s_.prepare(arm)
        s_.lock_start()
        srcs[file] = s_
    sources = {role: srcs[f] for role, f in SOURCES[name].items() if f in srcs}
    rest = {}
    for S in ("L", "R"):
        toe = arm.data.bones.get(f"J_Bip_{S}_ToeBase")
        rest[S] = (arm.data.bones[f"J_Bip_{S}_Foot"].head_local.z, toe.tail_local.z if toe else None)
    manifest = {}
    for clip in clips:
        plan = clip_plan(sources, name, clip)
        idxs = [0] if preview else range(len(plan))
        manifest[clip] = len(plan)
        for i in idxs:
            src, fr, drift, k, damp = plan[i]
            P.reset()
            D, _ = src.sample(fr, remove_drift=drift)
            if damp < 1.0 and sources.get("idle") is not None:       # mistura com a pose parada (slerp por osso)
                ref, _ = sources["idle"].sample(sources["idle"].f0)
                for bn in D:
                    if bn in ref:
                        D[bn] = ref[bn].to_quaternion().slerp(D[bn].to_quaternion(), damp).to_matrix()
            P.apply_world(D, extras_for(name, clip, k, near, far))
            bpy.context.view_layer.update()
            dz = -sole_height(arm, rest)
            P.set_hips_world((0, 0, dz))
            bpy.context.view_layer.update()
            sc.render.filepath = os.path.join(outdir, f"{clip}_{i:02d}.png")
            bpy.ops.render.render(write_still=True)
    walk = sources.get("walk")
    if walk is not None and "walk" in clips:
        manifest["strideMeters"] = round(abs(walk.drift.y) * walk.ratio, 3)
    return manifest


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    preview = "--preview" in argv
    argv = [a for a in argv if a != "--preview"]
    name, vrm, outdir = argv[0], os.path.abspath(argv[1]), os.path.abspath(argv[2])
    clips = argv[3:] or list(CLIPS)
    sc = setup_scene()
    tmp = os.path.join(tempfile.mkdtemp(), name + ".glb")
    shutil.copy(vrm, tmp)
    bpy.ops.import_scene.gltf(filepath=tmp)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    for o in list(meshes):
        if o.name.startswith("Icosphere"):
            bpy.data.objects.remove(o)
            meshes.remove(o)
    make_toon(meshes, name)
    if name == "ren":
        add_glasses(arm)
    if os.environ.get("VIEW") == "front":   # conferencia do rosto (olhando para +Y)
        cam = sc.camera
        cam.location = (0, -6, 1.1)
        cam.rotation_euler = (math.radians(90), 0, 0)
        if os.environ.get("ZOOM"):
            cam.data.ortho_scale = float(os.environ["ZOOM"])
            cam.location.z = float(os.environ.get("CAMZ", 1.6))
    P = Posable(arm)
    near, far = find_sides(arm)
    os.makedirs(outdir, exist_ok=True)
    if not os.environ.get("PROCEDURAL"):
        mclips = [c for c in (argv[3:] or MOCAP_CLIPS) if c in MOCAP_CLIPS]
        manifest = render_mocap(sc, arm, P, name, near, far, outdir, mclips, preview)
        with open(os.path.join(outdir, "manifest.json"), "w", encoding="utf-8") as fh:
            json.dump(manifest, fh, indent=1)
        print("OK", name, outdir, manifest)
        return
    for clip in clips:
        frames = [0] if preview else range(CLIPS[clip])
        for f in frames:
            if os.environ.get("REST"):
                P.reset()
                bpy.context.view_layer.update()
            else:
                pose(P, name, near, far, clip, f)
            sc.render.filepath = os.path.join(outdir, f"{clip}_{f:02d}.png")
            bpy.ops.render.render(write_still=True)
    print("OK", name, outdir)


if __name__ == "__main__":
    main()
