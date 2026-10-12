"""Prévia do Cenário 2: passagem 2 com uma porta fechada (luz vermelha) e outra aberta (verde)."""
from PIL import Image, ImageDraw, ImageFilter
bg = Image.open('out/Art/Cenario2/cenario2_fundo.png').convert('RGBA')
door = Image.open('out/Art/Cenario2/porta_vagao.png').convert('RGBA')
streak = Image.open('out/Art/Cenario2/luz_tunel.png').convert('RGBA')
EXT = 160
def py(p): return p + EXT
def streak_in(win, x, y):
    layer = Image.new('RGBA', bg.size, (0, 0, 0, 0)); layer.alpha_composite(streak, (x, py(y)))
    m = Image.new('L', bg.size, 0); ImageDraw.Draw(m).rectangle([win[0], py(win[2]), win[1], py(win[3])], fill=140)
    bg.paste(layer, (0, 0), Image.composite(layer, Image.new('RGBA', bg.size, (0, 0, 0, 0)), m).split()[3])
def put_door(sx, closure, lampc):
    x = sx + 10; top = py(498); h = 870 - 498
    vis_h = int(closure * h)
    if vis_h > 0: bg.alpha_composite(door.crop((0, door.height - vis_h, 70, door.height)), (x, top))
    cx = sx + 45; ly = py(498 - 34)
    glow = Image.new('RGBA', (140, 140), (0, 0, 0, 0)); ImageDraw.Draw(glow).ellipse([35, 35, 105, 105], fill=lampc + (130,))
    bg.alpha_composite(glow.filter(ImageFilter.GaussianBlur(16)), (cx - 70, ly - 70))
    ImageDraw.Draw(bg).rectangle([cx - 12, ly - 5, cx + 12, ly + 5], fill=lampc + (255,))
streak_in((4213, 4752, 236, 480), 4380, 320)
streak_in((2198, 2737, 236, 480), 2300, 380)
put_door(3515, 1.0, (205, 25, 15))
put_door(4115, 0.0, (90, 215, 115))
bg.crop((3050, 110, 3050 + 1760, 110 + 990)).convert('RGB').save('previa_cenario2.png')
