import bpy, sys, os
bpy.ops.wm.read_factory_settings(use_empty=True)
p = os.path.abspath(sys.argv[sys.argv.index("--")+1])
bpy.ops.import_scene.fbx(filepath=p)
arm = next(o for o in bpy.data.objects if o.type=="ARMATURE")
print("ARM", arm.name, "scale", tuple(arm.scale), "rot", tuple(round(v,3) for v in arm.rotation_euler))
print("BONES", len(arm.data.bones), [b.name for b in arm.data.bones][:30])
act = arm.animation_data.action if arm.animation_data else None
print("ACTION", act.name if act else None, "frames", tuple(act.frame_range) if act else None, "fps", bpy.context.scene.render.fps)
h = arm.data.bones[[b.name for b in arm.data.bones if b.name.endswith("Hips")][0]]
print("HIPS head", tuple(round(v,3) for v in h.head_local))
for n in ("LeftArm","LeftUpLeg","LeftFoot","Head","LeftHand","LeftToeBase"):
    for b in arm.data.bones:
        if b.name.endswith(n):
            print(n, tuple(round(v,3) for v in b.head_local), "->", tuple(round(v,3) for v in b.tail_local))
