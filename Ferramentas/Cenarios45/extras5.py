"""
Extras do Cenário 5 (gerados à parte do fundo):
  placa_admissoes.png, placa_avaliacoes.png, placa_demissoes.png, placa_financeiro.png  -> placas legíveis em cima dos arquivos
  chave.png                                                                              -> a chave reserva, na gaveta aberta
Uso:  python3 extras5.py <pasta_saida>   (ex.: Assets/Art/Cenario5)
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out/Art/Cenario5'
HERE = os.path.dirname(os.path.abspath(__file__))
FONT = os.path.join(HERE, '..', '..', 'Assets', 'UI', 'MainMenu', 'Fonts', 'Oswald-Regular.ttf')
SS = 4  # supersampling

os.makedirs(OUT, exist_ok=True)


def plate(label, name):
    w, h = 154, 40
    im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # sombra, moldura de latão gasta e esmalte escuro
    d.rounded_rectangle((3 * SS, 4 * SS, (w - 1) * SS, (h - 1) * SS), 5 * SS, fill=(0, 0, 0, 110))
    d.rounded_rectangle((1 * SS, 1 * SS, (w - 3) * SS, (h - 4) * SS), 5 * SS, fill=(120, 104, 72, 255))
    d.rounded_rectangle((4 * SS, 4 * SS, (w - 6) * SS, (h - 7) * SS), 3 * SS, fill=(28, 27, 24, 255))
    for x in (9, w - 12):                                                   # parafusos
        d.ellipse(((x - 2) * SS, (h // 2 - 4) * SS, (x + 2) * SS, (h // 2) * SS), fill=(150, 132, 92, 255))
    font = ImageFont.truetype(FONT, 17 * SS)
    spacing = 1.6 * SS
    widths = [d.textlength(ch, font=font) for ch in label]
    total = sum(widths) + spacing * (len(label) - 1)
    x = (w * SS - total) / 2
    y = (h - 4) * SS / 2
    for ch, cw in zip(label, widths):
        d.text((x, y), ch, font=font, fill=(226, 214, 186, 255), anchor='lm')
        x += cw + spacing
    im = im.resize((w, h), Image.LANCZOS)
    im.save(os.path.join(OUT, name + '.png'))


def key():
    w, h = 72, 30
    im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    brass, dark, shine = (196, 160, 82, 255), (96, 72, 30, 255), (246, 222, 150, 255)
    s = lambda *v: tuple(int(a * SS) for a in v)
    d.ellipse(s(3, 4, 25, 26), fill=dark)                                   # argola
    d.ellipse(s(4, 5, 24, 25), fill=brass)
    d.ellipse(s(10, 11, 18, 19), fill=(0, 0, 0, 0))
    d.rectangle(s(23, 12, 66, 18), fill=dark)                               # haste
    d.rectangle(s(23, 13, 65, 17), fill=brass)
    for x0, x1, depth in ((46, 51, 26), (54, 58, 24), (60, 65, 27)):         # dentes
        d.rectangle(s(x0, 17, x1, depth), fill=dark)
        d.rectangle(s(x0 + 1, 17, x1 - 1, depth - 1), fill=brass)
    d.line(s(26, 14, 62, 14), fill=shine, width=SS)                         # brilho
    d.arc(s(6, 7, 22, 23), 200, 300, fill=shine, width=SS)
    im = im.resize((w, h), Image.LANCZOS)
    glow = im.split()[3].filter(ImageFilter.GaussianBlur(2.5)).point(lambda a: int(a * 0.35))
    base = Image.new('RGBA', (w, h), (255, 226, 150, 0))
    base.putalpha(glow)
    out = Image.alpha_composite(base, im)
    out.save(os.path.join(OUT, 'chave.png'))


for label, name in (('ADMISSÕES', 'placa_admissoes'), ('AVALIAÇÕES', 'placa_avaliacoes'),
                    ('DEMISSÕES', 'placa_demissoes'), ('FINANCEIRO', 'placa_financeiro')):
    plate(label, name)
key()
print('ok extras5')
