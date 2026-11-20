"""Render de inspeção e funções de câmera/luz. blender -b kai.blend --python foto.py -- <saida.png> [yaw] [pose]

yaw: para onde o personagem olha (0 = câmera, 90 = perfil para a direita da tela).
"""
import math
import os
import sys

import bpy
from mathutils import Vector

AQUI = os.path.dirname(os.path.abspath(__file__))


def cena(res=(1024, 1536), escala=2.2, centro_z=0.92):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.eevee.taa_render_samples = 16
    w = bpy.data.worlds.new("w")
    sc.world = w
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = (0.05, 0.05, 0.05, 1)
    bg.inputs[1].default_value = 1.0
    cam_d = bpy.data.cameras.new("cam")
    cam_d.type = "ORTHO"
    cam_d.ortho_scale = escala
    cam = bpy.data.objects.new("cam", cam_d)
    sc.collection.objects.link(cam)
    cam.location = (0, -6, centro_z)
    cam.rotation_euler = (math.radians(90), 0, 0)
    sc.camera = cam
    # chave vinda do alto, à esquerda da tela: dá a sombra dura de um lado do rosto e do tronco
    sun = bpy.data.lights.new("sol", "SUN")
    sun.energy = 2.6
    sun.angle = math.radians(3)
    sun.use_shadow = False  # sem acne de sombra: o volume vem das dobras, as sombras de contato são pintadas
    so = bpy.data.objects.new("sol", sun)
    sc.collection.objects.link(so)
    so.rotation_mode = "QUATERNION"
    so.rotation_quaternion = Vector((0, 0, -1)).rotation_difference(Vector((0.62, 0.78, -0.75)).normalized())
    return sc, cam


def enquadra(cam, centro, escala):
    cam.data.ortho_scale = escala
    cam.location = (centro[0], -6, centro[1])


def main():
    a = sys.argv[sys.argv.index("--") + 1:]
    saida = a[0]
    yaw = float(a[1]) if len(a) > 1 else 0
    modo = a[2] if len(a) > 2 else "corpo"
    rig = bpy.data.objects["Kai"]
    rig.rotation_euler = (0, 0, math.radians(yaw))
    if modo == "cabeca":
        sc, cam = cena((1024, 1024))
        enquadra(cam, (0, 1.65), 0.42)
    else:
        sc, cam = cena()
    sc.render.filepath = saida
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
