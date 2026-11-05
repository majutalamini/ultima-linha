import bpy, sys, os, shutil, tempfile
sys.argv = [sys.argv[0], "--", "ren", "../VRoid/REN.vrm", "frames_vrm/dbg", "idle", "--preview"]
src = open("vrm_render.py", encoding="utf-8").read().replace("\nmain()\n", "\n")
exec(compile(src, "vrm_render.py", "exec"))
sc = setup_scene()
tmp = os.path.join(tempfile.mkdtemp(), "m.glb"); shutil.copy("../VRoid/REN.vrm", tmp)
bpy.ops.import_scene.gltf(filepath=tmp)
arm = next(o for o in bpy.data.objects if o.type=="ARMATURE")
for n in ("J_Adj_L_FaceEye","J_Adj_R_FaceEye","J_Bip_C_Head"):
    b = arm.data.bones[n]; print("BONE", n, tuple(round(v,3) for v in b.head_local), tuple(round(v,3) for v in b.tail_local))
print("ARM matrix", [tuple(round(v,3) for v in r) for r in arm.matrix_world])
face = bpy.data.objects["Face"]
import mathutils
vs = [face.matrix_world @ v.co for v in face.data.vertices]
print("FACE bbox", [round(min(v[i] for v in vs),3) for i in range(3)], [round(max(v[i] for v in vs),3) for i in range(3)])
parts = add_glasses(arm)
for p in parts[:2]: print("GLASS", p.name, tuple(round(v,3) for v in p.matrix_world.translation))
