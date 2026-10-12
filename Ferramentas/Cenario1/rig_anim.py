"""
Pose e animação dos personagens (mesma matemática do CharacterRig2D.cs), para prévia em GIF.
"""
import math, json
import numpy as np
from PIL import Image

R = 'out/Art/Cenario1/Rig/'
L = json.load(open(R + 'rig_layout.json'))
SK = L['_skeleton']
K = 2  # px por px base nas imagens exportadas

# personalidade (graus / px base)
STYLE = {
    'kai':   dict(lean=7, head=10, stride=22, knee=34, arm=16, bob=3.0, elbow=10, speed=1.0, shoulder=3),
    'souta': dict(lean=-1, head=-3, stride=26, knee=30, arm=22, bob=2.2, elbow=14, speed=1.0, shoulder=0),
    'ren':   dict(lean=12, head=16, stride=16, knee=30, arm=6, bob=2.0, elbow=62, speed=1.0, shoulder=6, clasp=True),
}


def pose(cid, phase, walk, air=0.0, t=0.0):
    """Ângulos (graus, + = anti-horário) de cada osso, e deslocamento do quadril."""
    s = STYLE[cid]
    sn, cs = math.sin(phase), math.cos(phase)
    breath = math.sin(t * 2.0)
    P = {}
    bob = (s['bob'] * (0.5 + 0.5 * math.cos(2 * phase))) * walk - 1.2 * walk
    P['hip_y'] = bob + breath * 0.4 * (1 - walk)
    P['torso'] = -s['lean'] - 2.0 * walk + breath * 0.6 * (1 - walk)   # inclina para a frente (sentido horário)
    P['head'] = -s['head'] + 1.5 * math.sin(2 * phase) * walk + breath * 0.8 * (1 - walk)
    for side, ph in (('f', phase), ('b', phase + math.pi)):
        a, c = math.sin(ph), math.cos(ph)
        thigh = s['stride'] * a * walk
        knee = s['knee'] * max(0.0, c) ** 1.4 * walk + 4
        P['thigh_' + side] = thigh
        P['shin_' + side] = -knee
        P['foot_' + side] = -(thigh - knee) * 0.75 + (8 * max(0.0, -c) * walk if a > 0 else 0)
        arm = -s['arm'] * a * walk
        if s.get('clasp'):
            P['upper_' + side] = 2 + arm * 0.3 + breath * 1.0
            P['fore_' + side] = s['elbow']
        else:
            P['upper_' + side] = arm + s['shoulder'] + breath * 0.8 * (1 - walk)
            P['fore_' + side] = s['elbow'] + 12 * max(0.0, -a) * walk
    if air > 0:  # pulo: pernas recolhidas, braços para trás/cima
        P['thigh_f'] = P['thigh_f'] * (1 - air) + 38 * air
        P['shin_f'] = P['shin_f'] * (1 - air) - 55 * air
        P['thigh_b'] = P['thigh_b'] * (1 - air) - 6 * air
        P['shin_b'] = P['shin_b'] * (1 - air) - 30 * air
        P['foot_f'] = P['foot_f'] * (1 - air) + 10 * air
        P['upper_f'] = P['upper_f'] * (1 - air) - 30 * air
        P['upper_b'] = P['upper_b'] * (1 - air) + 40 * air
        P['torso'] -= 4 * air
    return P


def rot(v, deg):
    a = math.radians(deg)
    return np.array([v[0] * math.cos(a) - v[1] * math.sin(a), v[0] * math.sin(a) + v[1] * math.cos(a)])


def compose(cid, P, size=(260, 420), origin=(110, 400)):
    """Monta a figura (vista de perfil, olhando para a direita). Retorna RGBA."""
    canvas = Image.new('RGBA', (size[0] * K, size[1] * K), (0, 0, 0, 0))
    hip_h = SK['thigh'] + SK['shin'] + SK['ankle']
    hip = np.array([0.0, hip_h + P['hip_y']])
    parts = []  # (ordem, nome, pos_junta, angulo)

    def place(name, joint, ang, order):
        parts.append((order, name, joint, ang))

    t_ang = P['torso']
    place('torso', hip, t_ang, 0)
    sh = hip + rot(SK['shoulder'], t_ang)
    nk = hip + rot(SK['neck'], t_ang)
    place('head', nk, t_ang + P['head'], 1)
    for side, o in (('b', -4), ('f', 2)):
        hp = hip + rot(SK['hip'], t_ang)
        a1 = P['thigh_' + side]
        place('thigh_' + side, hp, a1, -2 if side == 'b' else 1)
        kn = hp + rot((0, -SK['thigh']), a1)
        a2 = a1 + P['shin_' + side]
        place('shin_' + side, kn, a2, -2 if side == 'b' else 1)
        an = kn + rot((0, -SK['shin']), a2)
        place('foot_' + side, an, a2 + P['foot_' + side], -2 if side == 'b' else 1)
        u = t_ang + P['upper_' + side]
        place('upper_' + side, sh, u, o)
        el = sh + rot((0, -SK['upper']), u)
        place('fore_' + side, el, u + P['fore_' + side], o + (1 if side == 'f' else -1))
    for order, name, joint, ang in sorted(parts, key=lambda p: p[0]):
        img = Image.open(f"{R}{cid}_{name}.png").convert('RGBA')
        pv = L[cid][name]['pivot']
        px, py = pv[0] * img.width, (1 - pv[1]) * img.height
        big = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
        jx = (origin[0] + joint[0]) * K
        jy = (origin[1] - joint[1]) * K
        big.alpha_composite(img, (int(round(jx - px)), int(round(jy - py))))
        big = big.rotate(ang, center=(jx, jy), resample=Image.BICUBIC)
        canvas.alpha_composite(big)
    return canvas


if __name__ == '__main__':
    import sys
    bg = Image.open('cenario1_fundo.png').convert('RGBA').crop((1000, 450, 1520, 1290)).resize((520, 840))
    frames = []
    n = 24
    for i in range(n):
        ph = 2 * math.pi * i / n
        fr = bg.copy()
        for k, cid in enumerate(('kai', 'souta', 'ren')):
            fig = compose(cid, pose(cid, ph, 1.0, t=i / 12))
            fig = fig.resize((fig.width // 2 * 1, fig.height // 2 * 1), Image.LANCZOS)
            fr.alpha_composite(fig, (k * 170 - 40, 840 - fig.height - 20))
        frames.append(fr.convert('RGB'))
    frames[0].save('rig_walk.gif', save_all=True, append_images=frames[1:], duration=60, loop=0)
    # folha estática: parado, andando (2 fases), pulando
    sheet = Image.new('RGBA', (4 * 3 * 140, 440), (40, 38, 36, 255))
    x = 0
    for cid in ('kai', 'souta', 'ren'):
        for P in (pose(cid, 0, 0.0), pose(cid, math.pi / 2, 1.0), pose(cid, 3 * math.pi / 2, 1.0), pose(cid, 0, 0.3, air=1.0)):
            fig = compose(cid, P).resize((260, 420), Image.LANCZOS)
            sheet.alpha_composite(fig, (x - 40, 10))
            x += 140
    sheet.save('rig_sheet.png')
    print('ok')
