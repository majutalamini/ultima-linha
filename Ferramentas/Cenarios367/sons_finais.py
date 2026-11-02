"""Sons dos Cenários 3, 6 e 7. Tudo passa por passa-baixa (nada de chiado)."""
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

if __name__ == '__main__':
    for name, fn in SOUNDS.items():
        x = fn()
        write(name, x)
        # chiado: energia acima de 6 kHz
        from numpy.fft import rfft
        sp = np.abs(rfft(x)) ** 2
        fr = np.fft.rfftfreq(len(x), 1 / SR)
        print('ok', name, 'agudos %.5f' % (sp[fr > 6000].sum() / sp.sum()))
