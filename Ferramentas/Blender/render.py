"""Anima e renderiza um personagem em frames PNG (e folha de contato).

blender --background --python render.py -- <kai|souta|ren> <saida_dir> [clip ...]
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import chars

CLIPS = {"idle": 8, "walk": 8, "jump": 1, "fall": 1, "talk": 8}

# personalidade: (inclinacao do tronco, cabeca a frente, ombros, amplitude do passo)
STYLE = {
    "kai": dict(lean=9, head=12, shoulder=8, stride=26, arm=18, bob=0.018, speed=1.0),
    "souta": dict(lean=0, head=-2, shoulder=-3, stride=30, arm=24, bob=0.02, speed=1.0),
    "ren": dict(lean=12, head=16, shoulder=14, stride=20, arm=10, bob=0.012, speed=1.0),
}


def rot(j, deg):
    """Gira o pivo no plano XZ; deg > 0 = para frente (+X) para membros que pendem."""
    j.rotation_euler = (0, -math.radians(deg), 0)


def pose(rig, name, clip, f):
    J = rig.j
    s = STYLE[name]
    for e in J.values():
        e.rotation_euler = (0, 0, 0)
    hips_z = rig.root_z
    n = CLIPS[clip]
    ph = 2 * math.pi * f / n
    hips_dz = 0.0
    lean, head, sh = s["lean"], s["head"], s["shoulder"]

    if clip == "walk":
        A, K = s["stride"], 45
        for side, sgn in (("n", 1), ("f", -1)):
            a = A * math.sin(ph) * sgn
            rot(J[f"thigh_{side}"], a)
            flex = K * max(0.0, math.cos(ph) * sgn) + 6
            rot(J[f"knee_{side}"], -flex)
            rot(J[f"ankle_{side}"], -a * 0.35 + flex * 0.45)   # pe acompanha o chao
            rot(J[f"shoulder_{side}"], -s["arm"] * math.sin(ph) * sgn)
            rot(J[f"elbow_{side}"], 14 + 10 * max(0, -math.sin(ph) * sgn))
        hips_dz = -s["bob"] * abs(math.sin(ph)) + 0.006
        lean += 3
        rot(J["head"], head - lean * 0.5 + 2 * math.sin(2 * ph))
    elif clip == "idle":
        b = math.sin(ph)
        hips_dz = -0.004 * (1 - b)
        rot(J["chest"], lean * 0.5 + 1.2 * b)
        rot(J["head"], head - lean * 0.4 + 1.5 * b)
        for side in "nf":
            rot(J[f"shoulder_{side}"], 2 + 1.5 * b)
            rot(J[f"elbow_{side}"], 8)
        rot(J["knee_n"], -3)
        rot(J["knee_f"], -3)
        if name == "souta":     # olha o relogio (ultimos frames)
            k = max(0.0, math.sin(ph - 0.5)) if f >= 3 else 0.0
            rot(J["shoulder_n"], 2 + 45 * k)
            rot(J["elbow_n"], 8 + 70 * k)
            rot(J["head"], head + 14 * k)
        if name == "ren":       # tremor
            J["chest"].rotation_euler[2] = math.radians(1.2 * math.sin(ph * 3))
            rot(J["shoulder_n"], 20)
            rot(J["elbow_n"], 35)
            rot(J["shoulder_f"], 18)
            rot(J["elbow_f"], 38)
        if name == "kai":       # suspiro: cabeca sobe e desce
            sg = max(0.0, math.sin(ph))
            rot(J["head"], head - lean * 0.4 - 6 * sg)
            rot(J["chest"], lean * 0.5 - 3 * sg)
    elif clip == "talk":
        g = max(0.0, math.sin(ph))
        rot(J["chest"], lean * 0.5)
        rot(J["head"], head - lean * 0.4 + 4 * math.sin(2 * ph))
        rot(J["shoulder_n"], 2 + 30 * g)
        rot(J["elbow_n"], 20 + 55 * g)
        rot(J["shoulder_f"], 3)
        rot(J["elbow_f"], 10)
        rot(J["knee_n"], -3)
        rot(J["knee_f"], -3)
    elif clip == "jump":
        rot(J["thigh_n"], 35)
        rot(J["knee_n"], -70)
        rot(J["thigh_f"], -10)
        rot(J["knee_f"], -25)
        rot(J["shoulder_n"], -40)
        rot(J["elbow_n"], 35)
        rot(J["shoulder_f"], 30)
        rot(J["elbow_f"], 40)
        rot(J["head"], head - 4)
        lean += 4
    elif clip == "fall":
        rot(J["thigh_n"], 18)
        rot(J["knee_n"], -30)
        rot(J["thigh_f"], -20)
        rot(J["knee_f"], -15)
        rot(J["shoulder_n"], -70)
        rot(J["elbow_n"], 20)
        rot(J["shoulder_f"], -55)
        rot(J["elbow_f"], 15)
        rot(J["head"], head)
    if clip not in ("idle", "talk"):
        rot(J["chest"], lean * 0.6)
        rot(J["spine"], lean * 0.4)
    else:
        rot(J["spine"], lean * 0.4)
    J["hips"].location.z = hips_z + hips_dz
    bpy.context.view_layer.update()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    name, outdir = argv[0], os.path.abspath(argv[1])
    clips = argv[2:] or list(CLIPS)
    sc = chars.setup_scene()
    rig = chars.build(name)
    rig.root_z = rig.root.location.z
    os.makedirs(outdir, exist_ok=True)
    for clip in clips:
        for f in range(CLIPS[clip]):
            pose(rig, name, clip, f)
            sc.render.filepath = os.path.join(outdir, f"{clip}_{f:02d}.png")
            bpy.ops.render.render(write_still=True)
    print("OK", name, outdir)


main()
