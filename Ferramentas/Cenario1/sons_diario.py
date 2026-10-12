"""Sons do diário: caneta riscando o papel (loop) e página virando."""
import numpy as np, os
from sons import write, lowpass, t, SR, rng

def bandpass(x, lo, hi):
    return lowpass(x, hi) - lowpass(x, lo)

def caneta(sec=3.0):
    """Riscos curtos e irregulares, como letras sendo escritas."""
    n = int(sec * SR); tt = np.arange(n) / SR
    base = bandpass(rng.normal(0, 1, n), 1800, 7000)
    env = np.zeros(n); pos = 0.0
    while pos < sec:
        dur = 0.04 + rng.random() * 0.11
        i0, i1 = int(pos * SR), int(min(sec, pos + dur) * SR)
        seg = np.arange(i1 - i0) / SR
        env[i0:i1] += np.sin(np.pi * seg / dur) ** 0.7 * (0.5 + rng.random() * 0.5)
        pos += dur + rng.random() * 0.05
    x = base * env * (0.8 + 0.2 * np.sin(2 * np.pi * 7 * tt))
    f = int(0.15 * SR); w = np.linspace(0, 1, f)
    x[:f] = x[:f] * w + x[-f:] * (1 - w)
    return x[:-f]

def pagina():
    """Folha virando: sopro de papel com dois picos."""
    n = int(0.9 * SR); tt = np.arange(n) / SR
    x = bandpass(rng.normal(0, 1, n), 400, 5000)
    env = np.exp(-((tt - 0.22) / 0.10) ** 2) * 0.8 + np.exp(-((tt - 0.55) / 0.14) ** 2)
    crackle = (rng.random(n) > 0.995) * rng.normal(0, 1.5, n)
    return x * env + lowpass(crackle, 4000) * env

if __name__ == '__main__':
    write('caneta_escrevendo', caneta(), 0.5)
    write('pagina_virando', pagina(), 0.7)
    print('ok')
