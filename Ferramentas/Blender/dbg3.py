import bpy, os, shutil, tempfile
bpy.ops.wm.read_factory_settings(use_empty=True)
tmp = os.path.join(tempfile.mkdtemp(), "m.glb"); shutil.copy("../VRoid/REN.vrm", tmp)
bpy.ops.import_scene.gltf(filepath=tmp)
for o in bpy.data.objects:
    if o.type!="MESH": continue
    for m in o.data.materials:
        for n in m.node_tree.nodes:
            if n.type=="BSDF_PRINCIPLED":
                print("MAT", m.name[:40], "base", [round(v,2) for v in n.inputs["Base Color"].default_value], "linked", n.inputs["Base Color"].is_linked)
            if n.type=="TEX_IMAGE": print("   TEX", m.name[:30], n.image.name if n.image else None)
