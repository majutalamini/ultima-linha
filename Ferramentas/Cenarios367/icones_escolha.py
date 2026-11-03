"""
Ícones da escolha final (Cenário 7), no estilo dos botões de controle: triângulo, círculo, quadrado e xis,
em traço branco fino com um brilho leve. Saem em branco para o Unity colorir (selecionado / apagado).
Uso: python3 icones_escolha.py <pasta_saida>   (ex.: Assets/Art/Cenario7)
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out/Art/Cenario7'
SIZE, SS = 96, 4
os.makedirs(OUT, exist_ok=True)


def save(name, draw_fn):
    big = SIZE * SS
    im = Image.new('L', (big, big), 0)
    d = ImageDraw.Draw(im)
    draw_fn(d, big, int(5.5 * SS))
    line = im.resize((SIZE, SIZE), Image.LANCZOS)
    glow = line.filter(ImageFilter.GaussianBlur(3.2)).point(lambda a: int(a * 0.55))
    alpha = Image.fromarray(np.maximum(np.array(line), np.array(glow)))
    out = Image.new('RGBA', (SIZE, SIZE), (255, 255, 255, 0))
    out.putalpha(alpha)
    out.save(os.path.join(OUT, name + '.png'))


def triangle(d, s, w):
    m = s * 0.2
    h = (s - 2 * m) * math.sqrt(3) / 2
    top = (s - h) / 2 + s * 0.02
    pts = [(s / 2, top), (s - m, top + h), (m, top + h)]
    d.line(pts + [pts[0], pts[1]], fill=255, width=w, joint='curve')


def circle(d, s, w):
    m = s * 0.2
    d.ellipse((m, m, s - m, s - m), outline=255, width=w)


def square(d, s, w):
    m = s * 0.23
    d.rectangle((m, m, s - m, s - m), outline=255, width=w)


def cross(d, s, w):
    m = s * 0.24
    d.line((m, m, s - m, s - m), fill=255, width=w)
    d.line((s - m, m, m, s - m), fill=255, width=w)


for name, fn in (('icone_triangulo', triangle), ('icone_circulo', circle), ('icone_quadrado', square), ('icone_xis', cross)):
    save(name, fn)
print('ok icones')
