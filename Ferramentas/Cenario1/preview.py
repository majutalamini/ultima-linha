"""Prévia da cena montada (mesmas posições do Cenario1SceneBuilder), para conferir o encaixe."""
from PIL import Image, ImageDraw, ImageEnhance
A = 'out/Art/Cenario1/'
bg = Image.open(A + 'cenario1_fundo.png').convert('RGBA')
H = bg.height  # 1101
EXT = 160
img = bg.copy()


def py_to_img(py):  # y da imagem original -> y no fundo
    return py + EXT


def paste(name, x_px, y_img, pivot=(0.5, 0.5), scale=1.0, tint=None, alpha=1.0, rot=0):
    s = Image.open(A + name).convert('RGBA')
    if scale != 1.0:
        s = s.resize((max(1, int(s.width * scale)), max(1, int(s.height * scale))), Image.LANCZOS)
    if tint:
        r, g, b, a = s.split()
        s = Image.merge('RGBA', (r.point(lambda v: v * tint[0]), g.point(lambda v: v * tint[1]), b.point(lambda v: v * tint[2]), a.point(lambda v: v * alpha)))
    if rot:
        s = s.rotate(rot, expand=True, resample=Image.BICUBIC)
    x = int(x_px - s.width * pivot[0])
    y = int(y_img - s.height * (1 - pivot[1]))
    img.alpha_composite(s, (x, y))


FLOOR, CUSH, BACK, RACK = 870, 648, 515, 262
# bagageiros
for cx in (600, 3370):
    paste('bagageiro.png', cx, py_to_img(RACK), pivot=(0.5, (58 - 6) / 58))
# alavancas (base + cabo + lâmpada)
for x, py, lit in ((600, RACK, (1, .6, .2)), (2160, BACK, (1, .86, .6)), (3370, RACK, None)):
    yb = py_to_img(py)
    paste('alavanca_cabo.png', x, yb - 40, pivot=(0.5, 0), rot=38)
    paste('alavanca_base.png', x, yb, pivot=(0.5, 0))
    paste('lampada_soquete.png', x, yb - 118, pivot=(0.5, 0.5))
    if lit:
        paste('brilho.png', x, yb - 105, scale=0.55, tint=lit, alpha=0.8)
        paste('lampada.png', x, yb - 105, tint=lit)
    else:
        paste('lampada.png', x, yb - 105, tint=(0.22, 0.07, 0.05))
# letreiros
for cx in (1256, 2581, 3906):
    for i, c in enumerate([(0.22, 0.07, 0.05), (1, .86, .6), (0.22, 0.07, 0.05)]):
        paste('ponto.png', cx + (i - 1) * 77, py_to_img(171), scale=1.3, tint=c)
        if i == 1:
            paste('brilho.png', cx + (i - 1) * 77, py_to_img(171), scale=0.22, tint=c, alpha=0.8)
# porta: sinal
paste('sinal_porta.png', 3904, py_to_img(202))
paste('ponto.png', 3904, py_to_img(202), scale=1.0, tint=(0.85, 0.12, 0.08))
paste('brilho.png', 3904, py_to_img(202), scale=0.35, tint=(0.85, 0.12, 0.08), alpha=0.4)
# personagens
T = (0.84, 0.82, 0.8)
paste('Personagens/kai_cena.png', 520, py_to_img(FLOOR), pivot=(0.5, 0), tint=T)
paste('Personagens/kai_cena.png', 2160, py_to_img(BACK), pivot=(0.5, 0), tint=T)  # Kai no encosto (teste)
paste('Personagens/kai_cena.png', 3300, py_to_img(RACK), pivot=(0.5, 0), tint=T)  # Kai no bagageiro (teste)
s = Image.open(A + 'Personagens/souta_cena.png').transpose(Image.FLIP_LEFT_RIGHT); s.save('/tmp/souta_flip.png')
img.alpha_composite(Image.open('/tmp/souta_flip.png').resize((150, 350)), (1235 - 75, py_to_img(FLOOR) - 350))
r = Image.open(A + 'Personagens/ren_cena.png').transpose(Image.FLIP_LEFT_RIGHT)
img.alpha_composite(r.resize((150, 325)), (1685 - 75, py_to_img(FLOOR) - 325))
# halos das luminárias
for cx, w in ((558, 225), (1177, 197), (1883, 225), (2502, 197), (3208, 225), (3824, 190)):
    g = Image.open(A + 'brilho_tubo.png').convert('RGBA').resize((int(w * 1.7), 115))
    r_, g_, b_, a_ = g.split(); g = Image.merge('RGBA', (r_, g_, b_, a_.point(lambda v: v * 0.16)))
    img.alpha_composite(g, (int(cx - g.width / 2), int(py_to_img(111) - g.height / 2)))

# linhas dos colisores (para conferir)
d = ImageDraw.Draw(img)
plats = [(215, 1020, CUSH), (205, 1045, BACK), (1500, 2345, CUSH), (1460, 2370, BACK), (2825, 3670, CUSH), (2785, 3695, BACK),
         (436, 764, RACK), (3206, 3534, RACK)]
dbg = img.copy(); dd = ImageDraw.Draw(dbg)
for x0, x1, py in plats:
    dd.line([(x0, py_to_img(py)), (x1, py_to_img(py))], fill=(255, 200, 40, 255), width=3)
dd.line([(0, py_to_img(FLOOR)), (4230, py_to_img(FLOOR))], fill=(80, 255, 120, 255), width=3)
dd.line([(225, 0), (225, 1101)], fill=(255, 80, 80, 255), width=3)
dd.line([(4128, 0), (4128, 1101)], fill=(255, 80, 80, 255), width=3)
ImageEnhance.Brightness(dbg.convert('RGB')).enhance(1.6).resize((dbg.width // 3, dbg.height // 3)).save('preview_colisores.png')
img.convert('RGB').save('preview_full.png')
# recortes do tamanho da câmera (16.7 x 9.4 u = 1671 x 940 px)
for name, x0, y0 in (('cam_inicio', 0, 161), ('cam_meio', 1300, 161), ('cam_fim', 2560, 161), ('cam_alto', 2560, 0)):
    c = img.crop((x0, y0, x0 + 1671, y0 + 940)).convert('RGB')
    ImageEnhance.Brightness(c).enhance(1.25).resize((1003, 564)).save(f'{name}.png')
print('ok')
