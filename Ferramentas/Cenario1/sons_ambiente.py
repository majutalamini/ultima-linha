"""Ambiente do vagão SEM chiado (v6): só um zumbido grave e limpo + rangidos distantes do metal.
Nada de ruído branco/ar: a versão antiga tinha um "chiado" constante."""
import numpy as np
from sons import write, t, lowpass, metal_hit, SR, rng


def ambiente_limpo(sec=24.0):
    tt = t(sec)
    n = len(tt)
    # zumbido grave, senoidal pura (sem harmônicos agudos), respirando devagar
    hum = np.sin(2 * np.pi * 55 * tt) * 0.10 + np.sin(2 * np.pi * 82.5 * tt) * 0.03
    hum *= 1 + 0.25 * np.sin(2 * np.pi * tt / sec * 2)  # 2 ciclos por loop: emenda perfeita
    ev = np.zeros(n)
    for start in (3.1, 11.4, 18.7):
        h = metal_hit(2.0, 55 + rng.random() * 30, 0.7, (1, 1.5, 2.2)) * 0.22
        h = lowpass(lowpass(h, 500), 500)  # sem estalo agudo
        i = int(start * SR)
        ev[i:i + len(h)] += h[:n - i]
    return hum + ev


if __name__ == '__main__':
    write('ambiente_vagao', ambiente_limpo(), 0.45)
