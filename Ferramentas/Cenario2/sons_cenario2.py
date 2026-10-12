"""Sons do Cenário 2 (trem andando, portas entre vagões, freio). Sem chiado: tudo filtrado nos graves."""
import numpy as np
import sons
from sons import t, lowpass, metal_hit, env, SR, rng

sons.OUT = 'out/Audio/Cenario2'
import os
os.makedirs(sons.OUT, exist_ok=True)
write = sons.write


def lp(x, c, n=2):
    for _ in range(n):
        x = lowpass(x, c)
    return x


def clack(f0=70):
    h = metal_hit(0.5, f0 + rng.random() * 20, 0.12, (1, 1.8, 2.7)) * 0.5
    thump = lp(rng.normal(0, 1, len(h)), 160) * np.exp(-np.arange(len(h)) / SR / 0.05) * 6
    return lp(h, 900) + thump


def trem_andando(sec=8.8):
    n = int(sec * SR)
    rum = lp(rng.normal(0, 1, n), 85, 3) * 9
    tt = np.arange(n) / SR
    rum *= 1 + 0.25 * np.sin(2 * np.pi * tt / sec * 4)
    hum = np.sin(2 * np.pi * 47 * tt) * 0.08
    out = rum + hum
    # "tá-tum ... tá-tum": rodas passando nas emendas dos trilhos
    k = 0.0
    while k < sec - 0.6:
        for dt in (0.0, 0.13):
            c = clack()
            i = int((k + dt) * SR)
            out[i:i + len(c)] += c[:n - i] * 0.8
        k += 1.1
    f = int(0.5 * SR)
    w = np.linspace(0, 1, f)
    out[:f] = out[:f] * w + out[-f:] * (1 - w)
    return lp(out[:-f], 1400)


def porta_bate():
    a = metal_hit(1.2, 85, 0.35, (1, 1.6, 2.4, 3.9)) * 0.8
    thud = lp(rng.normal(0, 1, len(a)), 200) * np.exp(-np.arange(len(a)) / SR / 0.08) * 10
    return lp(a + thud, 2500)


def porta_aviso():
    out = np.zeros(int(0.5 * SR))
    for s in (0.0, 0.22):
        tt = t(0.13)
        b = (np.sin(2 * np.pi * 540 * tt) + 0.2 * np.sin(2 * np.pi * 1080 * tt)) * env(len(tt), 0.005, 0.07)
        i = int(s * SR)
        out[i:i + len(b)] += b
    return out


def porta_abre():
    tt = t(0.7)
    f = 120 + 90 * tt / 0.7
    whir = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * tt / 0.7) * 0.5
    clunk = metal_hit(0.4, 140, 0.08) * 0.4
    out = whir.copy()
    out[-len(clunk):] += lp(clunk, 1200)
    return out


def tranco():
    a = metal_hit(1.6, 55, 0.5, (1, 1.4, 2.1)) * 0.6
    boom = lp(rng.normal(0, 1, len(a)), 70, 2) * np.exp(-np.arange(len(a)) / SR / 0.35) * 18
    creak_t = t(0.9)
    creak = np.sin(2 * np.pi * (180 + 60 * np.sin(creak_t * 9)) * creak_t) * np.sin(np.pi * creak_t / 0.9) * 0.12
    out = lp(a, 700) + boom
    out[int(0.3 * SR):int(0.3 * SR) + len(creak)] += creak
    return out


def freio(sec=3.2):
    tt = t(sec)
    f = 1750 - 260 * (tt / sec) + 18 * np.sin(2 * np.pi * 7 * tt)
    ph = 2 * np.pi * np.cumsum(f) / SR
    screech = (np.sin(ph) + 0.35 * np.sin(2 * ph + 0.3)) * np.minimum(tt / 0.25, 1) * np.clip((sec - tt) / 0.7, 0, 1) * 0.22
    rum = lp(rng.normal(0, 1, len(tt)), 80, 3) * 10 * np.clip(1 - tt / sec, 0, 1)
    stop = metal_hit(1.0, 60, 0.4) * 0.5
    out = screech + rum
    out[-len(stop):] += lp(stop, 800)
    return out


def queda():
    tt = t(0.8)
    f = 260 * np.exp(-tt * 2.5) + 60
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-tt * 1.5) * 0.4
    thump = lp(rng.normal(0, 1, len(tt)), 120) * np.exp(-np.maximum(tt - 0.55, 0) / 0.08) * (tt > 0.55) * 14
    return tone + thump


def esmagado():
    a = porta_bate()
    crunch = lp(rng.normal(0, 1, int(0.25 * SR)), 700) * np.exp(-np.arange(int(0.25 * SR)) / SR / 0.06) * 3
    a[:len(crunch)] += crunch
    return a


def estacao(sec=10.0):
    tt = t(sec)
    n = len(tt)
    drone = np.sin(2 * np.pi * 50 * tt) * 0.06 + np.sin(2 * np.pi * 75 * tt) * 0.02
    out = drone.copy()
    for s in (2.2, 6.8):
        c = lp(metal_hit(1.8, 70, 0.8, (1, 1.5, 2.3)), 500) * 0.18
        i = int(s * SR)
        out[i:i + len(c)] += c[:n - i]
    return out


if __name__ == '__main__':
    write('trem_andando', trem_andando(), 0.55)
    write('porta_bate', porta_bate(), 0.85)
    write('porta_aviso', porta_aviso(), 0.35)
    write('porta_abre', porta_abre(), 0.4)
    write('tranco', tranco(), 0.8)
    write('freio', freio(), 0.6)
    write('queda', queda(), 0.7)
    write('esmagado', esmagado(), 0.9)
    write('estacao_ambiente', estacao(), 0.4)
