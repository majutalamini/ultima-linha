"""Pinta a textura do rosto do Kai (projeção frontal da cabeça). Roda no Python do sistema (Pillow).

python rosto_kai.py <saida.png> [aberto|piscando|fechado]

O quadrado cobre x de -0,10 a +0,10 m e z de 1,55 a 1,75 m; só traços ficam opacos, a pele vem do toon.
"""
import sys

from PIL import Image, ImageDraw, ImageFilter

S = 2048
SS = 2


def P(x, z):
    return ((0.5 + x / 0.2) * S * SS, (1 - (z - 1.55) / 0.2) * S * SS)


def layer():
    return Image.new("RGBA", (S * SS, S * SS), (0, 0, 0, 0))


def curve(pts, n=40):
    """Catmull-Rom por pontos (x, z)."""
    out = []
    pts = [pts[0]] + list(pts) + [pts[-1]]
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for k in range(n):
            t = k / n
            out.append(tuple(
                0.5 * (2 * p1[c] + (p2[c] - p0[c]) * t + (2 * p0[c] - 5 * p1[c] + 4 * p2[c] - p3[c]) * t * t
                       + (3 * p1[c] - p0[c] - 3 * p2[c] + p3[c]) * t * t * t) for c in (0, 1)))
    out.append(pts[-2])
    return out


def stroke(img, pts, width, color, taper=True):
    """Traço de pena: largura sobe e desce ao longo da curva."""
    d = ImageDraw.Draw(img)
    c = curve(pts)
    n = len(c)
    for i, (x, z) in enumerate(c):
        t = i / (n - 1)
        w = width * (0.25 + 0.75 * (4 * t * (1 - t)) ** 0.6 if taper else 1.0) * S * SS / 0.2
        px, py = P(x, z)
        r = max(1.0, w / 2)
        d.ellipse((px - r, py - r, px + r, py + r), fill=color)


def blob(pts, color, blur):
    img = layer()
    ImageDraw.Draw(img).polygon([P(*p) for p in curve(pts + [pts[0]], 16)], fill=color)
    return img.filter(ImageFilter.GaussianBlur(blur * SS))


def eye(base, sign, estado):
    """sign +1 = lado esquerdo dele (+X na imagem). Traços tirados da referência: pálpebra pesada, olho semicerrado."""
    cx, cz = 0.0345 * sign, 1.6415
    w, h = 0.0195, 0.0085  # meia-largura do olho, abertura
    top = [(cx - w * sign * 0.98, cz - 0.0012), (cx - w * 0.45 * sign, cz + h * 0.95), (cx + w * 0.3 * sign, cz + h * 1.05), (cx + w * sign, cz + 0.0014)]
    bot = [(cx + w * sign, cz + 0.0014), (cx + w * 0.3 * sign, cz - h * 0.72), (cx - w * 0.45 * sign, cz - h * 0.7), (cx - w * sign * 0.98, cz - 0.0012)]
    # olheira, marca principal do cansaço
    base.alpha_composite(blob([(cx - w * 1.1 * sign, cz - 0.004), (cx + w * 1.15 * sign, cz - 0.0035), (cx + w * 0.7 * sign, cz - 0.0125), (cx - w * 0.6 * sign, cz - 0.0115)], (86, 66, 70, 150), 9))
    base.alpha_composite(blob([(cx - w * 1.2 * sign, cz + 0.005), (cx + w * 1.2 * sign, cz + 0.0055), (cx + w * 0.9 * sign, cz + 0.0115), (cx - w * 0.8 * sign, cz + 0.0105)], (100, 82, 84, 95), 8))
    if estado == "fechado":
        stroke(base, [(cx - w * sign, cz - 0.001), (cx, cz - 0.0035), (cx + w * sign, cz + 0.0002)], 0.0022, (22, 18, 20, 255))
        return
    k = 0.45 if estado == "piscando" else 1.0
    ex = 0.0
    mask = layer()
    pts = [P(*p) for p in curve(top, 12)] + [P(*p) for p in curve(bot, 12)]
    if k < 1:
        pts = [(px, py + (P(cx, cz)[1] - py) * (1 - k)) for px, py in pts]
    ImageDraw.Draw(mask).polygon(pts, fill=(255, 255, 255, 255))
    olho = layer()
    d = ImageDraw.Draw(olho)
    d.rectangle((0, 0, S * SS, S * SS), fill=(206, 200, 194, 255))
    ix, iz = P(cx + ex - 0.0012 * sign, cz + 0.0002)
    r = 0.0086 / 0.2 * S * SS
    d.ellipse((ix - r, iz - r, ix + r, iz + r), fill=(58, 60, 66, 255))
    r2 = r * 0.82
    d.ellipse((ix - r2, iz - r2 + r * 0.1, ix + r2, iz + r2 + r * 0.1), fill=(112, 120, 126, 255))
    r3 = r * 0.44
    d.ellipse((ix - r3, iz - r3, ix + r3, iz + r3), fill=(20, 18, 20, 255))
    r4 = r * 0.2
    d.ellipse((ix + r * 0.25 - r4, iz - r * 0.45 - r4, ix + r * 0.25 + r4, iz - r * 0.45 + r4), fill=(214, 214, 216, 255))
    # sombra da pálpebra sobre o olho
    ImageDraw.Draw(olho).rectangle((0, 0, S * SS, P(0, cz + h * 0.42)[1]), fill=(40, 34, 38, 130))
    olho.putalpha(mask.split()[3])
    base.alpha_composite(olho)
    # linha pesada da pálpebra superior, com canto externo caído
    lid = [(p[0], cz + (p[1] - cz) * k) for p in top]
    stroke(base, lid, 0.0031, (20, 16, 18, 255))
    stroke(base, [(p[0], cz + (p[1] - cz) * k) for p in bot], 0.0011, (64, 50, 52, 225))
    # dobra da pálpebra
    stroke(base, [(cx - w * 0.8 * sign, cz + 0.0058), (cx, cz + h * 1.45), (cx + w * 0.95 * sign, cz + 0.0052)], 0.0009, (70, 54, 56, 210))
    # sobrancelha fina, quase reta, descendo para fora
    bz = cz + 0.0172
    stroke(base, [(cx - w * 1.05 * sign, bz + 0.0015), (cx - w * 0.1 * sign, bz + 0.0028), (cx + w * 1.2 * sign, bz - 0.0035)], 0.0024, (34, 28, 28, 255))


def main():
    out = sys.argv[1]
    estado = sys.argv[2] if len(sys.argv) > 2 else "aberto"
    img = layer()
    eye(img, 1, estado)
    eye(img, -1, estado)
    # nariz: só a sombra sob a ponta
    stroke(img, [(-0.0052, 1.6135), (-0.0005, 1.6112), (0.0052, 1.6135)], 0.0011, (96, 72, 70, 190))
    img.alpha_composite(blob([(-0.006, 1.6085), (0.006, 1.6085), (0.002, 1.6045), (-0.002, 1.6045)], (120, 92, 88, 70), 7))
    # boca pequena e fechada, canto levemente caído
    stroke(img, [(-0.0118, 1.5925), (-0.003, 1.5942), (0.004, 1.5944), (0.0125, 1.5915)], 0.0014, (58, 42, 44, 235))
    img.alpha_composite(blob([(-0.006, 1.5895), (0.006, 1.5895), (0.003, 1.5855), (-0.003, 1.5855)], (116, 88, 86, 75), 7))
    img.resize((S, S), Image.LANCZOS).save(out)


if __name__ == "__main__":
    main()
