"""Renderiza o busto (retrato de dialogo) de um personagem do VRoid, em 620x1024.

blender --background --python retrato.py -- <kai|souta|ren> <arquivo.vrm> <saida.png>
Depois, retrato_post.py aplica a luz vermelha de contorno e o fade (como o retrato original do jogo).
"""
import math
import os
import shutil
import sys
import tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector

import vrm_render as vr

# graus de inclinacao da cabeca (+ = para a frente) e giro do corpo/camera (3/4)
LOOK = {
    "kai": dict(head=10, turn=22),
    "souta": dict(head=-1, turn=22),
    "ren": dict(head=6, turn=22),
}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    name, vrm, out = argv[0], os.path.abspath(argv[1]), os.path.abspath(argv[2])
    sc = vr.setup_scene()
    sc.render.resolution_x, sc.render.resolution_y = 620, 1024
    sc.render.filter_size = 1.2
    tmp = os.path.join(tempfile.mkdtemp(), name + ".glb")
    shutil.copy(vrm, tmp)
    bpy.ops.import_scene.gltf(filepath=tmp)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    for o in list(meshes):
        if o.name.startswith("Icosphere"):
            bpy.data.objects.remove(o)
            meshes.remove(o)
    vr.make_toon(meshes, name)
    if name == "ren":
        vr.add_glasses(arm)

    # pose neutra: bracos para baixo e a cabecinha do personagem
    P = vr.Posable(arm)
    P.reset()
    for side in ("L", "R"):
        xs = arm.data.bones[f"J_Bip_{side}_UpperArm"].head_local.x
        P.rot(f"J_Bip_{side}_UpperArm", vr.Y, 76 if xs > 0 else -76)
    look = LOOK[name]
    P.rot("J_Bip_C_Neck", vr.X, look["head"] * 0.4)
    P.rot("J_Bip_C_Head", vr.X, look["head"] * 0.6)
    P.apply()
    bpy.context.view_layer.update()

    # camera de frente, girada para 3/4 (ortografica, enquadra cabeca e ombros)
    a = math.radians(look["turn"])
    cam = sc.camera
    cam.data.ortho_scale = 0.62
    cam.location = (-6 * math.sin(a), -6 * math.cos(a), 1.625)
    cam.rotation_euler = (math.radians(90), 0, -a)
    # luz: de frente e um pouco de cima, fria e fraca (o brilho vermelho vem do pos-processamento)
    for o in bpy.data.objects:
        if o.type == "LIGHT":
            o.data.energy = 1.6
            o.rotation_mode = "QUATERNION"
            o.rotation_quaternion = Vector((0, 0, -1)).rotation_difference(Vector((0.35, 0.8, -0.5)))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sc.render.filepath = out
    bpy.ops.render.render(write_still=True)
    print("OK", name, out)


main()
