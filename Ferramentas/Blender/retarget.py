"""Retarget de animações do Mixamo (FBX sem skin) para o esqueleto do VRoid (J_Bip_*).

Método: rotação "delta" em espaço do mundo, osso a osso.
  D_src(t)  = (rotação do osso de origem no quadro t) x (rotação de descanso dele)^-1
  D_alvo(t) = D_src(t) x C   onde C alinha a direção de descanso do osso alvo à do osso de origem
              (compensa T-poses um pouco diferentes: braço mais baixo, pernas mais abertas...)
Ossos do alvo sem par (dedos, cabelo) acompanham o pai. Quadril: altura escalada pela razão das pernas,
sem deslocamento para frente (animação "in place"). Depois os pés são "aterrados" por quadro.

Uso (dentro do Blender):
    src = MixamoSource("caminho/Walking.fbx"); src.prepare(target_arm)
    D, hips = src.sample(frame_float)         # rotações-mundo por osso do alvo + posição do quadril
"""
import math
import os

import bpy
from mathutils import Matrix, Vector

# osso do VRoid -> osso do Mixamo (sem o prefixo "mixamorig:"); {S} = lado
BONE_MAP = {
    "J_Bip_C_Hips": "Hips",
    "J_Bip_C_Spine": "Spine",
    "J_Bip_C_Chest": "Spine1",
    "J_Bip_C_UpperChest": "Spine2",
    "J_Bip_C_Neck": "Neck",
    "J_Bip_C_Head": "Head",
    "J_Bip_{S}_Shoulder": "{M}Shoulder",
    "J_Bip_{S}_UpperArm": "{M}Arm",
    "J_Bip_{S}_LowerArm": "{M}ForeArm",
    "J_Bip_{S}_Hand": "{M}Hand",
    "J_Bip_{S}_UpperLeg": "{M}UpLeg",
    "J_Bip_{S}_LowerLeg": "{M}Leg",
    "J_Bip_{S}_Foot": "{M}Foot",
    "J_Bip_{S}_ToeBase": "{M}ToeBase",
}


# proximo osso da cadeia (para medir a direcao anatomica)
CHILD = {
    "J_Bip_C_Hips": "J_Bip_C_Spine", "J_Bip_C_Spine": "J_Bip_C_Chest", "J_Bip_C_Chest": "J_Bip_C_UpperChest",
    "J_Bip_C_UpperChest": "J_Bip_C_Neck", "J_Bip_C_Neck": "J_Bip_C_Head",
    "J_Bip_{S}_Shoulder": "J_Bip_{S}_UpperArm", "J_Bip_{S}_UpperArm": "J_Bip_{S}_LowerArm",
    "J_Bip_{S}_LowerArm": "J_Bip_{S}_Hand",
    "J_Bip_{S}_UpperLeg": "J_Bip_{S}_LowerLeg", "J_Bip_{S}_LowerLeg": "J_Bip_{S}_Foot",
    "J_Bip_{S}_Foot": "J_Bip_{S}_ToeBase",
}
# pai na cadeia, usado pelas pontas (cabeca, mao, dedao) que nao tem filho mapeado
PARENT_OF = {
    "J_Bip_C_Head": "J_Bip_C_Neck",
    "J_Bip_L_Hand": "J_Bip_L_LowerArm", "J_Bip_R_Hand": "J_Bip_R_LowerArm",
    "J_Bip_L_ToeBase": "J_Bip_L_Foot", "J_Bip_R_ToeBase": "J_Bip_R_Foot",
}


def _rot(m):
    """Rotação pura (3x3) de uma matriz, ignorando escala uniforme."""
    return m.to_3x3().normalized()


def _find_bone(arm, short):
    for b in arm.data.bones:
        if b.name == short or b.name.endswith(":" + short):
            return b
    return None


def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    arm = next(o for o in new if o.type == "ARMATURE")
    for o in new:                       # sem skin não deve haver malhas, mas garante
        if o.type == "MESH":
            o.hide_render = True
    return arm


class MixamoSource:
    def __init__(self, path):
        self.path = path
        self.arm = import_fbx(path)
        act = self.arm.animation_data.action
        self.f0, self.f1 = act.frame_range
        self.pairs = {}          # osso alvo -> (osso de origem, C)
        self.src_hips_rest = None
        self.ratio = 1.0

    # ---------------------------------------------------------------- preparo
    def prepare(self, target):
        src = self.arm
        # lado: o "Left" do Mixamo (+X) corresponde ao J_Bip_L ou J_Bip_R do alvo, conforme o X dele
        tl = target.data.bones["J_Bip_L_UpperArm"].head_local.x
        sl = (src.matrix_world @ _find_bone(src, "LeftArm").head_local).x
        same = (tl > 0) == (sl > 0)
        side_map = {"L": "Left" if same else "Right", "R": "Right" if same else "Left"}
        mapping = {}                       # osso alvo -> osso de origem
        for tname, sname in BONE_MAP.items():
            for S in (("C",) if "{S}" not in tname else ("L", "R")):
                tn = tname.replace("{S}", S)
                sn = sname.replace("{M}", side_map.get(S, ""))
                tb, sb = target.data.bones.get(tn), _find_bone(src, sn)
                if tb is not None and sb is not None:
                    mapping[tn] = sb.name

        def head(arm, name):
            return arm.matrix_world @ arm.data.bones[name].head_local

        def anat_dir(arm, name_of, tn):
            """Direcao anatomica do osso: da articulacao dele ate a do proximo osso da cadeia.
            (A cauda dos ossos do importador glTF nao serve: aponta para cima.) Pontas herdam do pai."""
            seen = 0
            while tn is not None and seen < 4:
                child = CHILD.get(tn.replace("_L_", "_{S}_").replace("_R_", "_{S}_"))
                if child is not None:
                    child = child.replace("{S}", "L" if "_L_" in tn else "R" if "_R_" in tn else "C")
                    if child in mapping:
                        return (head(arm, name_of(child)) - head(arm, name_of(tn))).normalized()
                tn = PARENT_OF.get(tn)
                seen += 1
            return Vector((0, 0, 1))

        for tn, sn in mapping.items():
            d_t = anat_dir(target, lambda n: n, tn)
            d_s = anat_dir(src, lambda n: mapping[n], tn)
            self.pairs[tn] = (sn, d_t.rotation_difference(d_s).to_matrix())
        hs = _find_bone(src, "Hips")
        self.src_hips_rest = (src.matrix_world @ hs.matrix_local).translation.copy()
        th = target.data.bones["J_Bip_C_Hips"]
        self.tgt_hips_rest = (target.matrix_world @ th.matrix_local).translation.copy()
        self.ratio = self.tgt_hips_rest.z / max(0.01, self.src_hips_rest.z)
        self.rest_rot = {sb: _rot(src.matrix_world @ src.data.bones[sb].matrix_local) for sb, _ in self.pairs.values()}
        self.hips_name = hs.name
        # tendencia de deslocamento para frente ao longo do ciclo (removida nos andares)
        self.drift = self._hips_world_at(self.f1) - self._hips_world_at(self.f0)

    def _hips_world_at(self, frame):
        sc = bpy.context.scene
        sc.frame_set(int(math.floor(frame)), subframe=frame - math.floor(frame))
        pb = self.arm.pose.bones[self.hips_name]
        return (self.arm.matrix_world @ pb.matrix).translation.copy()

    # ---------------------------------------------------------------- amostragem
    def sample(self, frame, remove_drift=False):
        """Retorna (D, hips): rotações-mundo (3x3) por osso do alvo e a posição-mundo do quadril, já escalada."""
        sc = bpy.context.scene
        fl = math.floor(frame)
        sc.frame_set(int(fl), subframe=frame - fl)
        src = self.arm
        D = {}
        for tn, (sn, C) in self.pairs.items():
            pose = _rot(src.matrix_world @ src.pose.bones[sn].matrix)
            D[tn] = (pose @ self.rest_rot[sn].inverted()) @ C
        pos = (src.matrix_world @ src.pose.bones[self.hips_name].matrix).translation.copy()
        if remove_drift and (self.f1 - self.f0) > 0:
            pos -= self.drift * ((frame - self.f0) / (self.f1 - self.f0))
        rel = pos - self._hips0 if hasattr(self, "_hips0") else pos - self.src_hips_rest
        hips = Vector((rel.x * self.ratio, rel.y * self.ratio, self.tgt_hips_rest.z * (pos.z / self.src_hips_rest.z) - self.tgt_hips_rest.z))
        return D, hips

    def lock_start(self):
        """Fixa a origem do deslocamento horizontal no primeiro quadro (animação 'in place')."""
        self._hips0 = Vector((self._hips_world_at(self.f0).x, self._hips_world_at(self.f0).y, self.src_hips_rest.z))
