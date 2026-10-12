"""Prévia: mesmo layout que o IntroCutscene usa na Unity (1920x1080)."""
from PIL import Image, ImageDraw, ImageFont
F='/tmp/claude-0/fonts/Caveat-500.ttf'
DW,DH=1760,990        # diário na tela
book=Image.open('diario/diario_aberto.png').resize((DW,DH),Image.LANCZOS)
font=ImageFont.truetype(F,44); fdate=ImageFont.truetype(F,54)
ink=(32,34,50,255)
# áreas de texto (px dentro do diário): página esquerda e direita
AREAS=[(185,95,620,800),(DW//2+95,95,620,800)]
LH=52; GAP=20; DATEH=72
entries=["Mais um dia exaustivo no trabalho.","Eu já nem lembro da última vez que cheguei em casa sem sentir o corpo pesado.","Meu nome é Kai.","Faço o suficiente para não ser demitido.\nE pouco o bastante para ninguém me notar.","O Souta entrou na empresa no mesmo dia que eu.\nMesma sala de espera. Mesmo nervosismo. Mesmo sonho.","Hoje ele tem sala própria, reconhecimento, um futuro.\nEu tenho a mesma mesa de sempre.","Ele nunca me tratou diferente por isso.\nTalvez seja essa a parte que mais pesa.","Hoje, antes de sair, ele me esperou na porta.\n“Vamos juntos, Kai. Ainda dá tempo de pegar o último trem.”","Eu disse que sim.\nNão tinha forças para dizer não.","O último trem da noite.\nEu só queria fechar os olhos por um minuto..."]
dd=ImageDraw.Draw(book.copy())
def wrap(t,w):
    out=[]
    for para in t.split('\n'):
        line=''
        for word in para.split(' '):
            c=(line+' '+word).strip()
            if dd.textlength(c,font=font)<=w: line=c
            else: out.append(line); line=word
        out.append(line)
    return out
spreads=[]; spread=[[],[]]; side=0; used=DATEH+GAP; spread[0].append(('date','Dia 20 de outubro.'))
for e in entries:
    ls=wrap(e,AREAS[side][2]); need=len(ls)*LH+(GAP if spread[side] else 0)
    if used+need>AREAS[side][3]:
        side+=1; used=0
        if side>1: spreads.append(spread); spread=[[],[]]; side=0
        need=len(ls)*LH
    spread[side].append(('e',ls)); used+=need
spreads.append(spread)
print('abertas:',len(spreads),[[len(p) for p in s] for s in spreads])
for si,s in enumerate(spreads):
    b=book.copy(); d=ImageDraw.Draw(b)
    for side,items in enumerate(s):
        x0,y,w,h=AREAS[side]
        for k,(kind,v) in enumerate(items):
            if kind=='date': d.text((x0+8,y),v,font=fdate,fill=ink); y+=DATEH+GAP; continue
            if k>0 and items[k-1][0]!='date': y+=GAP
            for l in v: d.text((x0,y),l,font=font,fill=ink); y+=LH
    scr=Image.new('RGBA',(1920,1080),(0,0,0,255))
    r=b.rotate(-0.8,resample=Image.BICUBIC,expand=True)
    scr.alpha_composite(r,((1920-r.width)//2,(1080-r.height)//2+8))
    scr.convert('RGB').resize((960,540)).save(f'diario/prev_aberta{si+1}.png')
