"""Modelo toon dos personagens (Kai, Souta, Ren) para o Blender.

Uso (dentro do Blender): from chars import build; rig = build("kai")
O personagem olha para +X, a camera lateral fica em -Y.
Esqueleto = hierarquia de empties (pivos); cada peca de malha e filha de um pivo.
"""
import math
import bpy
from mathutils import Vector

# ---------------------------------------------------------------- paletas
CHARS = {
    "kai": dict(
        skin=(0.80, 0.66, 0.60), shirt=(0.66, 0.62, 0.60), tie=(0.17, 0.19, 0.27),
        pants=(0.09, 0.08, 0.10), shoes=(0.04, 0.04, 0.05), hair=(0.07, 0.055, 0.07),
        hair_style="messy", rolled=True, glasses=False, cardigan=None,
        height=1.0, build=0.95, eye=(0.35, 0.18, 0.2),
    ),
    "souta": dict(
        skin=(0.84, 0.70, 0.63), shirt=(0.74, 0.71, 0.70), tie=(0.07, 0.07, 0.09),
        pants=(0.09, 0.08, 0.09), shoes=(0.03, 0.03, 0.04), hair=(0.30, 0.22, 0.18),
        hair_style="neat", rolled=False, glasses=False, cardigan=None,
        height=1.04, build=1.0, eye=(0.25, 0.15, 0.1),
    ),
    "ren": dict(
        skin=(0.86, 0.73, 0.66), shirt=(0.72, 0.69, 0.66), tie=(0.12, 0.12, 0.15),
        pants=(0.11, 0.10, 0.11), shoes=(0.12, 0.06, 0.05), hair=(0.62, 0.52, 0.42),
        hair_style="soft", rolled=False, glasses=True, cardigan=(0.17, 0.13, 0.12),
        height=0.97, build=0.92, eye=(0.35, 0.3, 0.25),
    ),
}


# ---------------------------------------------------------------- materiais
def _toon_material(name, color, outline=False):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    if outline:
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs["Color"].default_value = (*color, 1)
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        mat.use_backface_culling = True
        return mat
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 1.0
    mix.inputs[7].default_value = (*color, 1)
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(diff.outputs[0], s2r.inputs[0])
    nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], mix.inputs[6])
    nt.links.new(mix.outputs[2], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    ramp.color_ramp.interpolation = "CONSTANT"
    el = ramp.color_ramp.elements
    el[0].position, el[0].color = 0.0, (0.34, 0.38, 0.52, 1)   # sombra fria
    el[1].position, el[1].color = 0.28, (0.66, 0.70, 0.82, 1)  # meio tom
    e3 = el.new(0.62)
    e3.color = (0.93, 0.92, 0.94, 1)                            # luz
    return mat


# ---------------------------------------------------------------- helpers
class Rig:
    def __init__(self):
        self.j = {}      # nome -> empty
        self.meshes = []
        self.root = None


def _joint(rig, name, loc, parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "PLAIN_AXES"
    e.empty_display_size = 0.05
    bpy.context.collection.objects.link(e)
    e.location = loc
    bpy.context.view_layer.update()
    if parent is not None:
        e.parent = parent
        e.matrix_parent_inverse = parent.matrix_world.inverted()
    rig.j[name] = e
    return e


def _part(rig, joint, mat, loc, scale, kind="sphere", outline=True, rot=(0, 0, 0), thick=0.012):
    """Elipsoide (ou caixa arredondada) filho de `joint`, em coordenadas de mundo."""
    if kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=18, radius=1, location=loc)
    else:
        bpy.ops.mesh.primitive_cube_add(size=2, location=loc)
    o = bpy.context.active_object
    o.scale = scale
    o.rotation_euler = tuple(math.radians(a) for a in rot)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.shade_smooth()
    if kind != "sphere":
        bev = o.modifiers.new("bev", "BEVEL")
        bev.width, bev.segments = 0.03, 4
        bpy.ops.object.modifier_apply(modifier="bev")
        bpy.ops.object.shade_smooth()
    o.data.materials.append(mat)
    o.parent = joint
    o.matrix_parent_inverse = joint.matrix_world.inverted()
    if outline:
        sol = o.modifiers.new("outline", "SOLIDIFY")
        sol.thickness = -thick
        sol.offset = 1
        sol.use_flip_normals = True
        sol.material_offset = 1
        o.data.materials.append(_toon_material("outline", (0.03, 0.025, 0.04), outline=True))
    rig.meshes.append(o)
    return o


# ---------------------------------------------------------------- construcao
def build(name):
    c = CHARS[name]
    H = c["height"]
    B = c["build"]
    rig = Rig()
    m = {k: _toon_material(f"{name}_{k}", c[k]) for k in
         ("skin", "shirt", "tie", "pants", "shoes", "hair")}
    m["eye"] = _toon_material(f"{name}_eye", (0.02, 0.02, 0.03))
    m["eyewhite"] = _toon_material(f"{name}_ew", (0.85, 0.84, 0.86))
    m["iris"] = _toon_material(f"{name}_iris", c["eye"])
    upper = _toon_material(f"{name}_upper", c["cardigan"] or c["shirt"])

    z_hip, z_knee, z_ank = 0.93 * H, 0.50 * H, 0.085
    z_sh, z_neck, z_head = 1.47 * H, 1.55 * H, 1.69 * H
    # pivos
    hips = _joint(rig, "hips", (0, 0, z_hip))
    rig.root = hips
    spine = _joint(rig, "spine", (0, 0, z_hip + 0.05), hips)
    chest = _joint(rig, "chest", (0, 0, z_hip + 0.30 * H), spine)
    neck = _joint(rig, "neck", (0.01, 0, z_neck), chest)
    head = _joint(rig, "head", (0.02, 0, z_neck + 0.04), neck)
    for s, y in (("n", -0.115 * B), ("f", 0.115 * B)):
        sh = _joint(rig, f"shoulder_{s}", (0, y, z_sh), chest)
        el = _joint(rig, f"elbow_{s}", (0, y, z_sh - 0.30 * H), sh)
        _joint(rig, f"wrist_{s}", (0, y, z_sh - 0.56 * H), el)
        th = _joint(rig, f"thigh_{s}", (0, y * 0.7, z_hip - 0.03), hips)
        kn = _joint(rig, f"knee_{s}", (0.01, y * 0.7, z_knee), th)
        _joint(rig, f"ankle_{s}", (0, y * 0.7, z_ank), kn)

    # tronco
    _part(rig, rig.j["hips"], m["pants"], (0, 0, z_hip + 0.02), (0.14 * B, 0.18 * B, 0.13))
    torso_mat = upper
    _part(rig, rig.j["chest"], torso_mat, (0, 0, z_hip + 0.30 * H), (0.145 * B, 0.2 * B, 0.28 * H))
    _part(rig, rig.j["spine"], m["shirt"] if not c["cardigan"] else upper,
          (0, 0, z_hip + 0.12), (0.14 * B, 0.18 * B, 0.13))
    if c["cardigan"]:  # camisa aparecendo na frente do cardigan
        _part(rig, rig.j["chest"], m["shirt"], (0.125 * B, 0, z_hip + 0.34 * H),
              (0.04, 0.06, 0.22 * H), outline=False)
    # gravata (lado da frente do peito)
    _part(rig, rig.j["chest"], m["tie"], (0.143 * B, 0, z_hip + 0.31 * H),
          (0.02, 0.04, 0.20 * H), outline=False)
    _part(rig, rig.j["chest"], m["tie"], (0.135 * B, 0, z_sh - 0.05 * H),
          (0.03, 0.05, 0.04), outline=False)
    # pescoco + cabeca
    _part(rig, rig.j["neck"], m["skin"], (0.01, 0, z_neck + 0.01), (0.06, 0.06, 0.08))
    _part(rig, rig.j["head"], m["skin"], (0.03, 0, z_head), (0.135, 0.125, 0.155))      # craneo/rosto
    _part(rig, rig.j["head"], m["skin"], (0.14, 0, z_head - 0.01), (0.035, 0.03, 0.045),  # nariz
          outline=False)
    _part(rig, rig.j["head"], m["skin"], (0.075, 0, z_head - 0.1), (0.07, 0.08, 0.055))  # queixo
    # olho (visto de perfil)
    ey = -0.115
    _part(rig, rig.j["head"], m["eyewhite"], (0.105, ey, z_head + 0.012), (0.036, 0.014, 0.034),
          outline=False)
    _part(rig, rig.j["head"], m["iris"], (0.122, ey - 0.004, z_head + 0.012), (0.02, 0.01, 0.026),
          outline=False)
    _part(rig, rig.j["head"], m["eye"], (0.128, ey - 0.007, z_head + 0.014), (0.011, 0.006, 0.016),
          outline=False)
    # sobrancelha
    _part(rig, rig.j["head"], m["hair"], (0.115, ey - 0.002, z_head + 0.06), (0.04, 0.009, 0.009),
          outline=False)
    _hair(rig, c, m, z_head)
    if c["glasses"]:
        gm = _toon_material("glass", (0.05, 0.05, 0.06))
        _part(rig, rig.j["head"], gm, (0.1, ey - 0.008, z_head + 0.012), (0.04, 0.006, 0.036),
              outline=False)
        _part(rig, rig.j["head"], gm, (0.06, ey - 0.008, z_head + 0.02), (0.05, 0.006, 0.006),
              outline=False)

    # bracos e pernas (n = proximo da camera, f = distante)
    sleeve = m["skin"] if c["rolled"] else upper
    for s, y in (("n", -0.115 * B), ("f", 0.115 * B)):
        yo = -0.016 if s == "n" else 0.016
        sh_y = y + yo
        outline = True
        _part(rig, rig.j[f"shoulder_{s}"], upper, (0, sh_y, z_sh - 0.14 * H),
              (0.062 * B, 0.062 * B, 0.17 * H), outline=outline)
        _part(rig, rig.j[f"elbow_{s}"], sleeve if not c["rolled"] else m["shirt"],
              (0, sh_y, z_sh - 0.27 * H), (0.058 * B, 0.058 * B, 0.075 * H), outline=outline)
        _part(rig, rig.j[f"elbow_{s}"], m["skin"] if c["rolled"] else sleeve,
              (0, sh_y, z_sh - 0.42 * H), (0.05 * B, 0.05 * B, 0.15 * H), outline=outline)
        _part(rig, rig.j[f"wrist_{s}"], m["skin"], (0.01, sh_y, z_sh - 0.60 * H),
              (0.045, 0.034, 0.065), outline=outline)
        ty = y * 0.7
        _part(rig, rig.j[f"thigh_{s}"], m["pants"], (0, ty, z_hip - 0.2 * H),
              (0.095 * B, 0.09 * B, 0.23 * H), outline=outline)
        _part(rig, rig.j[f"knee_{s}"], m["pants"], (0.005, ty, z_knee - 0.2 * H),
              (0.075 * B, 0.07 * B, 0.22 * H), outline=outline)
        _part(rig, rig.j[f"ankle_{s}"], m["shoes"], (0.07, ty, 0.05),
              (0.15, 0.06 * B, 0.055), outline=outline)
    return rig


def _hair(rig, c, m, zh):
    h = rig.j["head"]
    st = c["hair_style"]
    _part(rig, h, m["hair"], (0.0, 0, zh + 0.035), (0.148, 0.135, 0.145))   # calota
    _part(rig, h, m["hair"], (-0.07, 0, zh - 0.03), (0.07, 0.1, 0.1))      # nuca
    if st == "neat":   # penteado para o lado/trás, topete
        _part(rig, h, m["hair"], (0.07, 0, zh + 0.1), (0.075, 0.1, 0.05), rot=(0, -15, 0))
        _part(rig, h, m["hair"], (0.1, -0.06, zh + 0.07), (0.05, 0.04, 0.05))
    elif st == "messy":  # tufos desordenados
        for loc, sc, rot in [((0.06, -0.02, zh + 0.115), (0.07, 0.06, 0.05), (0, -25, 10)),
                             ((-0.02, 0.0, zh + 0.15), (0.06, 0.05, 0.05), (0, 15, 0)),
                             ((0.095, -0.07, zh + 0.07), (0.05, 0.035, 0.07), (20, -40, 0)),
                             ((-0.09, -0.02, zh + 0.06), (0.06, 0.06, 0.07), (0, 30, 0)),
                             ((0.0, -0.09, zh + 0.1), (0.07, 0.04, 0.05), (30, 0, 0))]:
            _part(rig, h, m["hair"], loc, sc, rot=rot)
    else:  # soft: franja caindo sobre os olhos
        _part(rig, h, m["hair"], (0.08, -0.04, zh + 0.07), (0.07, 0.06, 0.07), rot=(0, -20, 0))
        _part(rig, h, m["hair"], (0.1, -0.075, zh + 0.03), (0.05, 0.03, 0.08), rot=(10, -10, 0))
        _part(rig, h, m["hair"], (-0.03, 0, zh + 0.14), (0.1, 0.1, 0.04))


def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.render.resolution_x, sc.render.resolution_y = 256, 384
    sc.view_settings.view_transform = "Standard"
    # luz: de cima e frente, levemente a favor da camera
    ld = bpy.data.lights.new("sun", "SUN")
    ld.energy = 2.0
    lo = bpy.data.objects.new("sun", ld)
    bpy.context.collection.objects.link(lo)
    lo.rotation_euler = (math.radians(55), math.radians(15), math.radians(-60))
    # camera ortografica lateral (olha para +Y)
    cd = bpy.data.cameras.new("cam")
    cd.type = "ORTHO"
    cd.ortho_scale = 2.4
    co = bpy.data.objects.new("cam", cd)
    bpy.context.collection.objects.link(co)
    co.location = (0, -6, 1.1)
    co.rotation_euler = (math.radians(90), 0, 0)
    sc.camera = co
    return sc
