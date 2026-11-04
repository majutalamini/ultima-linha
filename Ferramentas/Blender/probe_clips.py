import bpy, sys, os, glob, math
d = os.path.abspath(sys.argv[sys.argv.index("--")+1])
for f in sorted(glob.glob(os.path.join(d, "*.fbx"))):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f)
    arm = next(o for o in bpy.data.objects if o.type=="ARMATURE")
    a = arm.animation_data.action
    f0, f1 = a.frame_range
    hips = next(b for b in arm.pose.bones if b.name.endswith("Hips"))
    sc = bpy.context.scene
    zs=[]; ys=[]
    n=int(f1-f0)
    for fr in range(int(f0), int(f1)+1, max(1,(int(f1-f0))//10)):
        sc.frame_set(fr)
        p=(arm.matrix_world @ hips.matrix).translation
        zs.append(round(p.z,2)); ys.append(round(p.y,2))
    print("CLIP", os.path.basename(f), "frames", int(f0), int(f1), "hipsZ", zs, "hipsY", ys)
