"""
Cenário 2 — os últimos vagões do trem, agora andando.
Monta o fundo panorâmico a partir da mesma imagem lateral do Notion usada no Cenário 1 (ref_a.png):
4 vagões ligados por 3 passagens (portas que batem + vão entre os vagões) e, no fim, a porta do
último vagão abrindo para a plataforma da estação terminal.

Gera em out/Art/Cenario2/:
  cenario2_fundo.png   fundo (≈7600×1101, 100 px/unidade; y=0 no pé da imagem)
  porta_vagao.png      folha da porta entre vagões (desce do teto e bate)
  luz_tunel.png        reflexo de luz do túnel passando nas janelas
e gera ../Cenario2Layout.cs com as medidas (para o builder da Unity).
"""
import json, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = 'out/Art/Cenario2'
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(22)

REF = 'ref_a.png' if os.path.exists('ref_a.png') else '../Cenario1/referencias/notion_vagao_lateral.png'
SRC = np.asarray(Image.open(REF).convert('RGB')).astype(np.float32)
H = SRC.shape[0]                  # 941
FLOOR = 870                       # py onde os pés pisam
DOOR_TOP = 498                    # topo da passagem (py)
SLAB = 90                         # espessura da parede do fim do vagão (px)
POST = 10                         # batente de cada lado
EXT = 160                         # teto extra, igual ao Cenário 1


def noise(h, w, s, seed):
    r = np.random.default_rng(seed).normal(0, 1, (h, w)).astype(np.float32)
    im = Image.fromarray(np.clip(r * 40 + 128, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(s))
    a = np.asarray(im).astype(np.float32) - 128
    return a / (a.std() + 1e-6)


def car(x0, x1):
    return SRC[:, x0:x1].copy()


def hazard(w, h, phase=0):
    yy, xx = np.mgrid[0:h, 0:w]
    s = ((xx + yy + phase) // 9) % 2
    c = np.where(s[..., None] == 0, np.array([150, 120, 30]), np.array([22, 20, 18])).astype(np.float32)
    c *= (0.75 + 0.25 * noise(h, w, 1.2, 5 + phase)[..., None].clip(-1, 1) * 0.5 + 0.25)
    return c


def slab(img, x, doorway_bg=None, lamp=True):
    """Parede do fim do vagão (vista de lado) com a passagem de porta. Desenha in-place em img (H×W×3)."""
    w = SLAB
    reg = img[:, x:x + w]
    n = noise(H, w, 5, x) * 2.5 + noise(H, w, 1.2, x + 1) * 1.2
    base = np.zeros((H, w, 3), np.float32) + np.array([31, 30, 33])
    base += n[..., None]
    base *= np.linspace(0.8, 1.05, H)[:, None, None]  # mais escuro em cima
    # painéis verticais e rebites
    for k in range(4, w, 22):
        base[:, k:k + 1] *= 0.65
    for py in range(40, DOOR_TOP - 10, 70):
        for px in (18, w - 18):
            base[py:py + 3, px:px + 3] += 35
    # luz do vagão batendo na borda
    ramp = np.linspace(1.25, 0.8, w)[None, :, None]
    base *= ramp
    # batentes
    base[:, :POST] = base[:, :POST] * 1.3 + 8
    base[:, w - POST:] = base[:, w - POST:] * 1.25 + 6
    # trilho da porta e caixa da lâmpada de aviso
    base[DOOR_TOP - 12:DOOR_TOP, POST:w - POST] = np.array([12, 12, 13])
    if lamp:
        cx = w // 2
        base[DOOR_TOP - 46:DOOR_TOP - 22, cx - 16:cx + 16] = np.array([18, 16, 16])
        base[DOOR_TOP - 42:DOOR_TOP - 26, cx - 12:cx + 12] = np.array([60, 18, 16])
    # a passagem (vão da porta) mostra o que está atrás
    inner = doorway_bg if doorway_bg is not None else np.zeros((FLOOR - DOOR_TOP, w - 2 * POST, 3)) + 10
    base[DOOR_TOP:FLOOR, POST:w - POST] = inner
    # soleira com faixa de perigo
    base[FLOOR - 4:FLOOR + 10, :] = hazard(w, 14, x % 9)
    base[FLOOR + 10:, :] *= 0.45
    reg[:] = base


def bellows(w):
    """Sanfona entre os vagões: dobras de borracha escuras, uma luz vermelha de emergência no alto."""
    img = np.zeros((H, w, 3), np.float32) + np.array([6, 6, 7])
    xx = np.arange(w)
    fold = 0.35 + 0.65 * np.abs(np.sin(xx * np.pi / 18.0)) ** 2
    top, bot = 120, FLOOR
    band = np.zeros((H, w))
    band[top:bot] = fold[None, :]
    curve = np.sin(np.linspace(0, np.pi, bot - top))[:, None] * 0.35 + 0.65
    band[top:bot] *= curve
    img += (band * 17)[..., None] * np.array([1.0, 0.95, 0.95])
    img += noise(H, w, 4, w)[..., None] * 1.5
    img *= np.linspace(1.0, 0.55, H)[:, None, None]  # chão da passagem mais escuro
    # moldura de cima e de baixo
    img[top - 14:top] = np.array([24, 23, 25])
    img[bot - 10:bot] = np.array([20, 19, 20])
    # luz vermelha de emergência no teto da passagem
    yy, xx2 = np.mgrid[0:H, 0:w]
    cx = w / 2
    d = np.sqrt(((xx2 - cx) / (w * 0.7)) ** 2 + ((yy - top) / 380.0) ** 2)
    glow = np.clip(1 - d, 0, 1) ** 2
    img += glow[..., None] * np.array([80, 10, 8])
    img[top - 6:top + 4, int(cx) - 14:int(cx) + 14] = np.array([170, 30, 24])
    return img


def pit(w, coupler=None):
    """O vão no chão: escuridão, trilhos passando borrados e o engate entre os vagões."""
    h = H - FLOOR
    img = np.zeros((h, w, 3), np.float32) + np.array([4, 4, 5])
    # dormentes passando (borrados pelo movimento)
    sl = noise(h, w, 0.5, 77 + w)
    sl = np.asarray(Image.fromarray(np.clip(sl * 30 + 128, 0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(6))).astype(np.float32) / 255
    img[40:, :] += (sl[40:, :, None] * 10)
    for ry, b in ((48, 46), (64, 30)):
        img[ry:ry + 3, :] = np.array([b, b, b + 4])
    # engate (barra escura atravessando o vão, mais embaixo)
    img[18:30, :] = np.array([20, 19, 19])
    img[18:20, :] += 14
    if coupler:
        c0, c1 = coupler
        img[0:20, c0 + 10:c1 - 10] = np.array([26, 25, 25])
    return img


def checker(w, h, seed):
    yy, xx = np.mgrid[0:h, 0:w]
    p = ((np.sin(xx * 0.8 + yy * 0.8) > 0.6) | (np.sin(xx * 0.8 - yy * 0.8) > 0.6)).astype(np.float32)
    c = np.zeros((h, w, 3), np.float32) + np.array([46, 45, 47]) + p[..., None] * 16
    c += noise(h, w, 2, seed)[..., None] * 2
    c[:3] += 28  # beirada iluminada
    return c


def gangway(plate_a, holes, plate_b, coupler=0):
    """Passagem: [parede A][chapa A][vão][engate][vão][chapa B][parede B] (larguras em px)."""
    widths = [SLAB, plate_a] + ([holes[0], coupler, holes[1]] if coupler else [holes[0]]) + [plate_b, SLAB]
    w = sum(widths)
    img = np.zeros((H, w, 3), np.float32)
    inner_w = w - 2 * SLAB
    bel = bellows(inner_w)
    img[:, SLAB:SLAB + inner_w] = bel
    meta = {'width': w, 'holes': [], 'plates': [], 'slabs': [0, w - SLAB]}
    x = SLAB
    segs = [('plate', plate_a)]
    if coupler:
        segs += [('hole', holes[0]), ('coupler', coupler), ('hole', holes[1])]
    else:
        segs += [('hole', holes[0])]
    segs += [('plate', plate_b)]
    hole_span = None
    for kind, sw in segs:
        if kind == 'hole':
            img[FLOOR - 4:, x:x + sw] = pit(sw)[min(0, 0):][:H - FLOOR + 4] if False else np.vstack([np.zeros((4, sw, 3)) + 4, pit(sw)])[:H - FLOOR + 4]
            meta['holes'].append([x, x + sw])
        else:
            top = FLOOR - 4 if kind == 'plate' else FLOOR + 2
            th = 22 if kind == 'plate' else 16
            img[top:top + th, x:x + sw] = checker(sw, th, x)
            img[top + th:, x:x + sw] = np.array([6, 6, 7])
            meta['plates'].append([x, x + sw, kind])
        x += sw
    # paredes (a passagem mostra a sanfona atrás)
    door_bg_a = bel[DOOR_TOP:FLOOR, :SLAB - 2 * POST] * 0.55
    door_bg_b = bel[DOOR_TOP:FLOOR, -(SLAB - 2 * POST):] * 0.55
    slab(img, 0, door_bg_a)
    slab(img, w - SLAB, door_bg_b)
    return img, meta


def station(w):
    """Plataforma da estação terminal, vista pela porta do último vagão."""
    img = np.zeros((H, w, 3), np.float32) + np.array([7, 7, 7])
    # parede do fundo com azulejos encardidos
    yy, xx = np.mgrid[0:H, 0:w]
    tile = ((xx % 46 < 2) | (yy % 46 < 2)).astype(np.float32)
    grime = np.clip(noise(H, w, 9, 400) * 0.5 + 0.5, 0, 1.5)
    img += (1 - tile)[..., None] * np.array([13, 14, 12]) * (0.6 + 0.4 * grime[..., None])
    img += noise(H, w, 4, 402)[..., None] * 1.5
    # escorrido escuro descendo da parede
    drip = (np.clip(noise(1, w, 14, 403)[0] - 0.7, 0, None) * 0.6)[None, :] * np.clip((yy - 200) / 500, 0, 1)
    img *= (1 - np.clip(drip, 0, 0.6))[..., None]
    # luz fria/amarelada fraca de uma luminária
    d = np.sqrt(((xx - w * 0.55) / (w * 0.7)) ** 2 + ((yy - 170) / 480) ** 2)
    img += (np.clip(1 - d, 0, 1) ** 1.8)[..., None] * np.array([46, 44, 32])
    img[150:158, int(w * 0.3):int(w * 0.8)] = np.array([150, 148, 128])
    # placa "TERMINAL"
    sign = Image.new('RGB', (230, 54), (24, 26, 30))
    d2 = ImageDraw.Draw(sign)
    try:
        f = ImageFont.truetype(next(p for p in ('fonts/IMFellEnglish-Regular.ttf', '/home/claude/c1/fonts/IMFellEnglish-Regular.ttf',
                                          '../../Assets/UI/MainMenu/Fonts/IMFellEnglish-Regular.ttf') if os.path.exists(p)), 34)
    except Exception:
        f = ImageFont.load_default()
    d2.rectangle([0, 0, 229, 53], outline=(70, 72, 76), width=2)
    tw = d2.textlength('TERMINAL', font=f)
    d2.text(((230 - tw) / 2, 6), 'TERMINAL', font=f, fill=(196, 192, 180))
    sx = int(w * 0.18)
    img[260:314, sx:sx + 230] = np.asarray(sign).astype(np.float32) * 0.55
    img[210:260, sx + 40:sx + 43] = 40
    img[210:260, sx + 187:sx + 190] = 40
    # piso da plataforma + faixa amarela tátil (como na imagem do Notion)
    img[FLOOR - 4:FLOOR + 4] = np.array([58, 56, 50])
    strip = np.zeros((14, w, 3), np.float32) + np.array([118, 92, 24])
    sx2 = np.arange(w)
    dots = ((sx2 % 12) < 5)[None, :] & ((np.arange(14) % 7) < 4)[:, None]
    strip[dots] += 30
    img[FLOOR - 4:FLOOR + 10, :min(w, 160)] = strip[:, :min(w, 160)]
    img[FLOOR + 10:] = np.array([20, 19, 18])
    img[FLOOR + 10:] += noise(H - FLOOR - 10, w, 3, 401)[..., None] * 2
    return img


def build():
    pieces, layout = [], {}
    x = 0

    def add(arr, name=None, extra=None):
        nonlocal x
        if name:
            layout[name] = {'x': x, **(extra or {})}
        pieces.append(arr)
        x += arr.shape[1]

    # vagão A (começo, com a divisória da ponta à esquerda)
    add(car(0, 1640), 'carA', {'w': 1640, 'src0': 0})
    g, m = gangway(100, [150], 120)
    add(g, 'gang1', m)
    add(car(315, 1640), 'carB', {'w': 1325, 'src0': 315})
    g, m = gangway(100, [110, 110], 120, coupler=70)
    add(g, 'gang2', m)
    add(car(315, 1640), 'carC', {'w': 1325, 'src0': 315})
    g, m = gangway(100, [260], 120)
    add(g, 'gang3', m)
    add(car(315, 1470), 'carD', {'w': 1155, 'src0': 315})
    # fim do último vagão: parede com a porta para fora + plataforma
    st = station(520)
    end = np.zeros((H, SLAB, 3), np.float32)
    slab(end, 0, st[DOOR_TOP:FLOOR, 30:30 + SLAB - 2 * POST] * 1.0)
    add(end, 'endSlab', {'w': SLAB})
    add(st, 'station', {'w': 520})
    pano = np.concatenate(pieces, axis=1)

    # luz: terceiro vagão mais escuro (luzes falhando), passagens um pouco mais escuras
    W = pano.shape[1]
    dim = np.ones(W, np.float32)
    c = layout['carC']['x']
    dim[c:c + 1325] *= 0.82
    pano *= dim[None, :, None]

    # teto extra (espelha e escurece, igual ao Cenário 1)
    band = pano[:EXT][::-1]
    band = np.asarray(Image.fromarray(np.clip(band, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(14))).astype(np.float32)
    fade = (np.linspace(0.0, 0.45, EXT) ** 1.6)[:, None, None]
    basec = np.array([2, 2, 2.5], np.float32)
    ext = basec * (1 - fade) + band * fade * 0.6
    out = np.concatenate([ext, pano], axis=0)
    # pontas escurecem
    lr = np.ones(W, np.float32)
    lr[:120] = np.linspace(0.55, 1, 120)
    lr[-80:] = np.linspace(1, 0.4, 80)
    out = out * lr[None, :, None] + basec * (1 - lr[None, :, None])
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(f'{OUT}/cenario2_fundo.png', optimize=True)
    layout['total'] = W
    layout['height'] = out.shape[0]
    return layout


def door_sprite():
    """Folha da porta: aço escuro, uma janelinha alta, faixa de perigo na ponta de baixo. 70×374 px."""
    w, h = SLAB - 2 * POST, FLOOR - DOOR_TOP + 2
    img = np.zeros((h, w, 3), np.float32) + np.array([40, 39, 42])
    img += noise(h, w, 4, 900)[..., None] * 2.5
    img *= np.linspace(1.1, 0.85, h)[:, None, None]
    img[:, 2:4] += 18          # quina iluminada
    img[:, w - 4:] *= 0.55
    img[h // 2 - 2:h // 2 + 2, 6:w - 6] *= 0.55   # junta no meio
    # janelinha
    img[46:128, 18:w - 18] = np.array([10, 10, 12])
    img[46:48, 18:w - 18] += 30
    img[60:120, 26:30] += 10
    img[h - 46:h - 6] = hazard(w, 40, 3)
    img[h - 6:] = np.array([16, 15, 15])
    for _ in range(10):
        x0 = rng.integers(4, w - 10); y0 = rng.integers(140, h - 70); ln = rng.integers(10, 40)
        img[y0:y0 + ln, x0:x0 + 1] += 18
    a = np.full((h, w), 255, np.uint8)
    Image.fromarray(np.dstack([np.clip(img, 0, 255).astype(np.uint8), a]), 'RGBA').save(f'{OUT}/porta_vagao.png')
    return w, h


def streak_sprite():
    w, h = 420, 70
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    core = np.exp(-((yy - h / 2) / 7) ** 2) * np.clip(1 - np.abs(xx - w * 0.3) / (w * 0.3), 0, 1) ** 0.7
    tail = np.exp(-((yy - h / 2) / 11) ** 2) * np.clip((xx - w * 0.25) / (w * 0.75), 0, 1) ** 0.5 * np.clip(1 - (xx - w * 0.25) / (w * 0.75), 0, 1) * 0.8
    a = np.clip(core + tail * 0.6, 0, 1)
    rgb = np.dstack([np.full((h, w), 220), np.full((h, w), 232), np.full((h, w), 255)])
    Image.fromarray(np.dstack([rgb, a * 255]).astype(np.uint8), 'RGBA').save(f'{OUT}/luz_tunel.png')


# janelas e luminárias na imagem original (px): para a luz do túnel e os halos
WINDOWS = [(318, 862, 236, 480), (1110, 1210, 262, 498), (1290, 1390, 262, 498)]
TUBES = [(558, 225), (1177, 197)]


def write_cs(lay):
    cars = ['carA', 'carB', 'carC', 'carD']
    wins, tubes = [], []
    for c in cars:
        x, w, s0 = lay[c]['x'], lay[c]['w'], lay[c]['src0']
        for (a, b, y0, y1) in WINDOWS:
            a2, b2 = max(a - s0, 8), min(b - s0, w - 8)
            if b2 - a2 > 60:
                wins.append((x + a2, x + b2, y0, y1))
        for (cx, tw) in TUBES:
            if 0 < cx - s0 < w:
                tubes.append((x + cx - s0, tw))
    gangs = []
    for g in ['gang1', 'gang2', 'gang3']:
        G = lay[g]; x = G['x']
        gangs.append(dict(slabA=x, slabB=x + G['slabs'][1],
                          holes=[(x + a, x + b) for a, b in G['holes']],
                          plates=[(x + a, x + b) for a, b, k in G['plates']]))
    L = []
    L.append('// Gerado por Ferramentas/Cenario2/cenario2.py — não edite à mão.')
    L.append('using UnityEngine;')
    L.append('namespace UltimaLinha.EditorTools')
    L.append('{')
    L.append('    /// <summary>Medidas do fundo do Cenário 2 (px da imagem; y a partir do topo da imagem original).</summary>')
    L.append('    internal static class Cenario2Layout')
    L.append('    {')
    L.append(f"        public const float LevelWidth = {lay['total']}f, LevelHeight = {lay['height']}f, ImageHeight = 941f;")
    L.append(f"        public const float FloorPy = {lay['floor']}f, DoorTopPy = {lay['door']['top']}f;")
    L.append(f"        public const float Slab = {lay['door']['slab']}f, Post = {lay['door']['post']}f;")
    L.append('        public static readonly float[] CarX = { ' + ', '.join(f"{lay[c]['x']}f" for c in cars) + ' };')
    L.append('        public static readonly float[] CarW = { ' + ', '.join(f"{lay[c]['w']}f" for c in cars) + ' };')
    L.append('        // passagens: parede A, parede B (x da borda esquerda de cada parede), vãos e chapas')
    L.append('        public static readonly float[] SlabA = { ' + ', '.join(f"{g['slabA']}f" for g in gangs) + ' };')
    L.append('        public static readonly float[] SlabB = { ' + ', '.join(f"{g['slabB']}f" for g in gangs) + ' };')
    L.append('        public static readonly Vector2[][] Holes =')
    L.append('        {')
    for g in gangs:
        L.append('            new[] { ' + ', '.join(f'new Vector2({a}f, {b}f)' for a, b in g['holes']) + ' },')
    L.append('        };')
    L.append('        public static readonly Vector2[][] Plates =')
    L.append('        {')
    for g in gangs:
        L.append('            new[] { ' + ', '.join(f'new Vector2({a}f, {b}f)' for a, b in g['plates']) + ' },')
    L.append('        };')
    L.append(f"        public const float EndSlabX = {lay['endSlab']['x']}f, StationX = {lay['station']['x']}f, StationW = {lay['station']['w']}f;")
    L.append('        // janelas (x0, x1, y0, y1) por onde passa a luz do túnel')
    L.append('        public static readonly Vector4[] Windows =')
    L.append('        {')
    for w in wins:
        L.append(f'            new Vector4({w[0]}f, {w[1]}f, {w[2]}f, {w[3]}f),')
    L.append('        };')
    L.append('        // luminárias do teto (centro x, largura)')
    L.append('        public static readonly Vector2[] Tubes = { ' + ', '.join(f'new Vector2({t[0]}f, {t[1]}f)' for t in tubes) + ' };')
    L.append('    }')
    L.append('}')
    open('Cenario2Layout.cs', 'w').write('\n'.join(L) + '\n')


if __name__ == '__main__':
    lay = build()
    dw, dh = door_sprite()
    streak_sprite()
    lay['door'] = {'w': dw, 'h': dh, 'top': DOOR_TOP, 'slab': SLAB, 'post': POST}
    lay['floor'] = FLOOR
    json.dump(lay, open('cenario2_layout.json', 'w'), indent=1)
    write_cs(lay)
    print('ok', lay['total'])
