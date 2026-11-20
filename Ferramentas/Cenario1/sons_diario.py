"""Sons do diário: caneta riscando o papel (loop) e página virando."""
import numpy as np, os
from sons import write, lowpass, t, SR, rng

def bandpass(x, lo, hi):
    return lowpass(x, hi) - lowpass(x, lo)

def caneta(sec=4.0):
    """Lápis bem macio no papel, quase um sussurro: ruído só nos graves e médios (180-1100 Hz, sem nada agudo),
    traços longos que se emendam (nada de ritmo picotado) e volume baixo. O loop emenda sem corte: o ruído é
    filtrado no domínio da frequência (circular) e o envelope dá a volta no fim."""
    n = int(sec * SR)
    spec = np.fft.rfft(rng.normal(0, 1, n))
    f = np.fft.rfftfreq(n, 1 / SR)
    shape = np.exp(-0.5 * (np.log(np.maximum(f, 1.0) / 420.0) / 0.55) ** 2)  # sino em torno de 420 Hz
    shape[f > 1400] *= np.exp(-(f[f > 1400] - 1400) / 250.0)                  # corta o chiado de vez
    base = np.fft.irfft(spec * shape, n)
    tt = np.arange(n) / SR
    env = np.zeros(n)
    pos = 0.0
    while pos < sec:                                                          # traços longos e sobrepostos
        dur = 0.35 + rng.random() * 0.45
        c = pos + dur / 2
        for k in (-1, 0, 1):                                                  # dá a volta: o loop não estala
            env += np.exp(-0.5 * ((tt - c - k * sec) / (dur / 3.2)) ** 2) * (0.6 + rng.random() * 0.25)
        pos += dur * (0.55 + rng.random() * 0.25)
    env = 0.55 + 0.45 * env / env.max()                                       # nunca some, nunca pica
    return base * env

def pagina():
    """Folha virando: sopro de papel com dois picos."""
    n = int(0.9 * SR); tt = np.arange(n) / SR
    x = bandpass(rng.normal(0, 1, n), 400, 5000)
    env = np.exp(-((tt - 0.22) / 0.10) ** 2) * 0.8 + np.exp(-((tt - 0.55) / 0.14) ** 2)
    crackle = (rng.random(n) > 0.995) * rng.normal(0, 1.5, n)
    return x * env + lowpass(crackle, 4000) * env

if __name__ == '__main__':
    write('caneta_escrevendo', caneta(), 0.07)
    write('pagina_virando', pagina(), 0.7)
    print('ok')
