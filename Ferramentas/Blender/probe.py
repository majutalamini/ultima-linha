import bpy, sys, os, math
bpy.ops.wm.read_factory_settings(use_empty=True)
p = sys.argv[sys.argv.index("--")+1]
bpy.ops.import_scene.gltf(filepath=p)
arms=[o for o in bpy.data.objects if o.type=='ARMATURE']
meshes=[o for o in bpy.data.objects if o.type=='MESH']
print("ARMATURES",[a.name for a in arms])
print("MESHES",len(meshes),[m.name for m in meshes][:20])
if arms:
    a=arms[0]; print("BONES",len(a.data.bones),[b.name for b in a.data.bones][:60])
    print("dims", max((o.dimensions.z for o in meshes)))
