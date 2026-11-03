"""Sons dos Cenários 3, 6 e 7 (e os ajustes do corpo do Cenário 5). Tudo passa por passa-baixa (nada de chiado)."""
from sons_salas import *
import sons_salas as S

def loop_fade(x, f=0.5):
    n = int(f * SR)
    x = x.copy()
    x[:n] = x[:n] * np.linspace(0, 1, n) + x[-n:] * np.linspace(1, 0, n)
    return x[:-n]

def ambiente_estacao():
    n = int(9 * SR); tt = t(9)
    x = lp(noise(n), 160) * 0.5 + np.sin(2 * np.pi * 50 * tt) * 0.08 + np.sin(2 * np.pi * 100 * tt) * 0.03
    for at in (1.8, 5.3, 7.4):
        place(x, lp(metal(0.4, 900, 0.05, (1, 2.4)), 3000) * 0.05, at)      # pingo
    return loop_fade(x)

def ambiente_tunel():
    n = int(9 * SR); tt = t(9)
    x = lp(noise(n), 110) * 0.7 + np.sin(2 * np.pi * 38 * tt) * 0.14 * (1 + 0.3 * np.sin(2 * np.pi * 0.2 * tt))
    for at in (0.9, 3.7, 6.2):
        place(x, lp(metal(0.5, 760, 0.07, (1, 2.4)), 2800) * 0.06, at)
    return loop_fade(x)

def ambiente_plataforma():
    n = int(9 * SR); tt = t(9)
    gust = 0.5 + 0.5 * np.sin(2 * np.pi * 0.17 * tt + 1)
    x = lp(noise(n), 260) * (0.3 + 0.5 * gust) + np.sin(2 * np.pi * 55 * tt) * 0.05
    return loop_fade(x)

def alarme_loop():
    tt = t(2.0)
    f = 330 + 90 * (np.sin(2 * np.pi * 0.5 * tt) > 0)
    x = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * 0.5 + np.sin(2 * np.pi * np.cumsum(f) / SR)
    gate = (np.sin(2 * np.pi * 2 * tt) > -0.2).astype(float)
    return lp(x, 1400) * gate

def metro_ativa():
    buf = np.zeros(int(4.5 * SR)); tt = t(4.5)
    f = 40 + 90 * (1 - np.exp(-tt / 1.3))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.minimum(tt / 0.6, 1) * 0.9
    x += lp(noise(len(tt)), 220) * 0.4 * np.minimum(tt / 1.0, 1)
    buf += x
    place(buf, thump(0.5, 70, 0.14) * 1.1, 0.05)
    place(buf, lp(metal(0.8, 300, 0.2, (1, 2.4, 4.2)), 2500) * 0.4, 0.2)
    place(buf, thump(0.6, 55, 0.2) * 1.0, 1.4)
    return buf

def contagem_tic():
    return lp(metal(0.25, 880, 0.04, (1, 2.0)), 3200) * env(int(0.25 * SR), 0.002, 0.05)

def trem_buzina():
    tt = t(2.4)
    x = (np.sin(2 * np.pi * 196 * tt) + 0.7 * np.sin(2 * np.pi * 247 * tt) + 0.3 * np.sin(2 * np.pi * 392 * tt))
    e = np.minimum(tt / 0.08, 1) * np.where(tt < 1.9, 1, np.exp(-(tt - 1.9) / 0.15))
    return lp(x, 2200) * e

def trem_aproxima():
    n = int(6 * SR); tt = t(6)
    amp = (tt / 6) ** 2.2
    x = lp(noise(n), 220) * amp * 1.0 + np.sin(2 * np.pi * (35 + 25 * tt / 6) * tt) * amp * 0.9
    k = 7 + 10 * tt / 6
    x *= 0.7 + 0.3 * np.sin(2 * np.pi * k * tt)
    return x

def tiro():
    buf = np.zeros(int(2.0 * SR))
    place(buf, lp(noise(int(0.25 * SR)) * env(int(0.25 * SR), 0.001, 0.04), 4500) * 1.0, 0)
    place(buf, thump(0.5, 110, 0.12, 0.7) * 1.2, 0)
    place(buf, lp(noise(int(1.6 * SR)) * env(int(1.6 * SR), 0.02, 0.4), 700) * 0.18, 0.05)    # eco na plataforma
    return buf

def empurrao():
    buf = np.zeros(int(1.0 * SR))
    place(buf, lp(noise(int(0.25 * SR)) * env(int(0.25 * SR), 0.01, 0.07), 900) * 0.7, 0)
    place(buf, thump(0.3, 100, 0.07) * 0.9, 0.05)
    return buf

def queda_trilho():
    buf = np.zeros(int(1.2 * SR))
    place(buf, lp(noise(int(0.3 * SR)) * env(int(0.3 * SR), 0.004, 0.08), 1200) * 0.8, 0.0)
    place(buf, thump(0.6, 60, 0.18) * 1.3, 0.0)
    place(buf, lp(metal(0.7, 330, 0.2, (1, 2.4)), 2200) * 0.25, 0.02)
    return buf

def papel_virar():
    n = int(0.35 * SR)
    return lp(noise(n) * env(n, 0.03, 0.08), 2400) * 0.5

SOUNDS = dict(ambiente_estacao=ambiente_estacao, ambiente_tunel=ambiente_tunel, ambiente_plataforma=ambiente_plataforma,
              alarme_loop=alarme_loop, metro_ativa=metro_ativa, contagem_tic=contagem_tic, trem_buzina=trem_buzina,
              trem_aproxima=trem_aproxima, tiro=tiro, empurrao=empurrao, queda_trilho=queda_trilho, papel_virar=papel_virar)

# ---------------- Ajustes (novembro): corpo do Cenário 5 e finais do Cenário 7 ----------------
def pegajoso(i):
    """Passo grudando no sangue: estalo molhado, sucção e o sapato descolando."""
    r = np.random.default_rng(500 + i)
    n = int(0.42 * SR); tt = t(0.42)
    x = bp(r.normal(0, 1, n), 260 + 40 * i, 1500 + 200 * i) * env(n, 0.008, 0.07) * 0.9
    f = (190 - 20 * i) * np.exp(-tt / 0.12) + 70
    x += np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, 0.004, 0.05) * 0.45            # sucção
    tack = bp(r.normal(0, 1, int(0.12 * SR)), 600, 2600) * env(int(0.12 * SR), 0.002, 0.025) * 0.5
    place(x, tack, 0.13 + 0.03 * i)                                                     # descolando
    bub = np.sin(2 * np.pi * (900 + 150 * i) * t(0.05) * (1 + t(0.05) * 8)) * env(int(0.05 * SR), 0.002, 0.012) * 0.18
    place(x, bub, 0.2 + 0.02 * i)
    return lp(x, 3800)

def pegajoso_1(): return pegajoso(1)
def pegajoso_2(): return pegajoso(2)
def pegajoso_3(): return pegajoso(3)

def cabeca_solta():
    buf = np.zeros(int(1.6 * SR))
    n = int(0.3 * SR); tt = t(0.3)
    tear = bp(noise(n), 350, 2200) * (0.6 + 0.4 * np.sin(2 * np.pi * 34 * tt)) * env(n, 0.02, 0.12)
    place(buf, tear * 0.8, 0.0)
    place(buf, pegajoso(4) * 0.7, 0.05)
    place(buf, thump(0.4, 95, 0.09) * 0.9, 0.3)
    for k, at in enumerate((0.52, 0.68, 0.8, 0.9)):
        place(buf, thump(0.25, 120 + 10 * k, 0.05) * (0.45 - 0.09 * k), at)
    return lp(buf, 3500)

def _sirene(freq, sec, approach=True):
    tt = t(sec)
    ph = 2 * np.pi * np.cumsum(freq) / SR
    x = np.sin(ph) + 0.35 * np.sin(3 * ph) + 0.15 * np.sin(5 * ph)
    near = np.clip(tt / sec, 0, 1) ** 1.6 if approach else np.ones_like(tt)
    far, close = lp(x, 900), lp(x, 3600)
    y = far * (1 - near) + close * near
    amp = 0.18 + 0.82 * near
    y = y * amp + lp(noise(len(tt)), 200) * 0.06                        # rua ao fundo
    fade = np.minimum(tt / 0.8, 1) * np.minimum((sec - tt) / 0.6, 1)
    return y * fade

def sirene_ambulancia():
    sec = 11.0; tt = t(sec)
    hi = (np.floor(tt / 0.48) % 2) == 0
    f = np.where(hi, 960.0, 720.0)
    f = lp(f, 40) * (1 + 0.01 * np.clip(1 - tt / sec, 0, 1))            # leve Doppler
    return _sirene(f, sec)

def sirene_policia():
    sec = 11.0; tt = t(sec)
    wail = 650 + 700 * (0.5 - 0.5 * np.cos(2 * np.pi * tt / 3.2))
    yelp = 750 + 550 * (0.5 - 0.5 * np.cos(2 * np.pi * tt / 0.28))
    f = np.where(tt < 6.4, wail, yelp)
    return _sirene(lp(f, 60), sec)

def plantao():
    """Vinheta de plantão do telejornal."""
    buf = np.zeros(int(2.6 * SR))
    def brass(f0, sec, vol):
        tt = t(sec)
        saw = sum(np.sin(2 * np.pi * f0 * k * tt) / k for k in range(1, 9))
        return lp(saw, 2600) * np.minimum(tt / 0.02, 1) * np.exp(-tt / (sec * 0.6)) * vol
    place(buf, thump(0.8, 70, 0.3) * 1.0, 0.0)
    for at, f0 in ((0.0, 220), (0.16, 330), (0.32, 440)):
        place(buf, brass(f0, 0.22, 0.5), at)
    for f0 in (220, 277.2, 330, 440):
        place(buf, brass(f0, 1.8, 0.35), 0.52)
    place(buf, thump(0.8, 60, 0.35) * 0.9, 0.52)
    return buf

def corpo_cai():
    buf = np.zeros(int(1.0 * SR))
    place(buf, lp(noise(int(0.2 * SR)) * env(int(0.2 * SR), 0.01, 0.06), 1400) * 0.5, 0.0)
    place(buf, thump(0.5, 75, 0.13) * 1.0, 0.08)
    place(buf, thump(0.3, 110, 0.06) * 0.4, 0.3)
    return buf

SOUNDS.update(pegajoso_1=pegajoso_1, pegajoso_2=pegajoso_2, pegajoso_3=pegajoso_3, cabeca_solta=cabeca_solta,
              sirene_ambulancia=sirene_ambulancia, sirene_policia=sirene_policia, plantao=plantao, corpo_cai=corpo_cai)

if __name__ == '__main__':
    for name, fn in SOUNDS.items():
        x = fn()
        write(name, x)
        # chiado: energia acima de 6 kHz
        from numpy.fft import rfft
        sp = np.abs(rfft(x)) ** 2
        fr = np.fft.rfftfreq(len(x), 1 / SR)
        print('ok', name, 'agudos %.5f' % (sp[fr > 6000].sum() / sp.sum()))
