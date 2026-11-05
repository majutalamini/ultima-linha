import bpy, sys, os, shutil, tempfile
bpy.ops.wm.read_factory_settings(use_empty=True)
p = os.path.abspath(sys.argv[sys.argv.index("--")+1])
t = os.path.join(tempfile.mkdtemp(), "m.glb"); shutil.copy(p, t)
bpy.ops.import_scene.gltf(filepath=t)
for o in bpy.data.objects:
    if o.type=="MESH": print("MESH", o.name, [m.name for m in o.data.materials])
