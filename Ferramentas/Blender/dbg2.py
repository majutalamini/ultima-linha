import bpy, os, shutil, tempfile
bpy.ops.wm.read_factory_settings(use_empty=True)
tmp = os.path.join(tempfile.mkdtemp(), "m.glb"); shutil.copy("../VRoid/REN.vrm", tmp)
bpy.ops.import_scene.gltf(filepath=tmp)
body = bpy.data.objects["Body"]
for m in body.data.materials:
    if "Tops" in m.name:
        for n in m.node_tree.nodes:
            if n.type=="TEX_IMAGE" and n.image:
                im=n.image; print("IMG", im.name, im.size[:], im.colorspace_settings.name)
                px=list(im.pixels); import collections
                h=collections.Counter()
                for i in range(0,len(px),4*7):
                    r,g,b,a=px[i:i+4]
                    if a>0.5: h[round((0.3*r+0.6*g+0.1*b)*10)]+=1
                print("HIST(lum*10 linear)", sorted(h.items()))
